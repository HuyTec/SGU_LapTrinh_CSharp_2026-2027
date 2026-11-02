"""Local 50-student load check. Creates dedicated users/session; no production data."""
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timedelta, timezone
from pathlib import Path
import io
import csv
import json
import math
import time
import threading
import uuid
import smoke_http as api


def main():
    count = 50
    run = uuid.uuid4().hex[:8]
    admin = api.login_staff("admin")
    officer = api.login_staff("officer")
    print("Preparing 50 isolated demo students...", flush=True)
    names = [f"load_{run}_{i}" for i in range(count)]
    stream = io.StringIO(newline="")
    writer = csv.writer(stream)
    writer.writerow(["UserName", "FullName", "StudentCode", "Email", "Password"])
    for i, name in enumerate(names):
        writer.writerow([name, f"Thí sinh tải {i}", f"P{run}_{i}", "", api.PASSWORD])
    api.call("/api/admin/users/import", "POST", {"csv": stream.getvalue()}, admin)
    users, _, _ = api.call("/api/admin/users?search=" + run + "&pageSize=100", token=admin)
    assert users["total"] == count
    catalog, _, _ = api.call("/api/admin/catalog", token=officer)
    session_resource = next(x for x in catalog["resources"] if x["key"] == "sessions")
    paper = int(next(x for x in session_resource["fields"] if x["name"] == "examId")["choices"][0]["value"])
    now = datetime.now(timezone.utc)
    sid = api.save(officer, "sessions", {"examId": paper, "title": "Kiểm thử tải " + run,
        "opensAt": (now - timedelta(minutes=1)).isoformat(), "closesAt": (now + timedelta(minutes=15)).isoformat(),
        "durationMinutes": 10, "shuffleQuestions": True, "shuffleOptions": True, "reviewPolicy": "ScoreOnly"})
    for user in users["items"]:
        api.save(officer, "candidates", {"sessionId": sid, "studentId": user["id"]})
    api.action(officer, "sessions", sid, "activate")

    def prepare(name):
        browser = api.login_student(name)
        details, _, _ = api.call(f"/Sessions/Details/{sid}", opener=browser)
        page, url, _ = api.call(f"/Sessions/Start/{sid}", "POST", opener=browser,
            form={"__RequestVerificationToken": api.Inputs(details).token})
        attempt = int(url.rsplit("/", 1)[1])
        parsed = api.Inputs(page)
        qid, options = next(iter(parsed.questions.items()))
        return browser, attempt, parsed.token, qid, options[0]["value"]

    # Preparation is deliberately bounded; the measured phases use 50 parallel callers.
    with ThreadPoolExecutor(max_workers=8) as pool:
        clients = list(pool.map(prepare, names))
    print("Measuring 50 concurrent answer saves...", flush=True)

    save_barrier = threading.Barrier(count)
    list_barrier = threading.Barrier(count)

    def save_answer(client):
        browser, attempt, token, qid, option = client
        save_barrier.wait(timeout=30)
        started = time.perf_counter()
        api.call(f"/Attempts/Save/{attempt}", "POST", {"questionId": qid, "optionId": option, "isFlagged": False},
            opener=browser, csrf=token)
        return (time.perf_counter() - started) * 1000

    with ThreadPoolExecutor(max_workers=count) as pool:
        save_ms = list(pool.map(save_answer, clients))

    def list_sessions(client):
        list_barrier.wait(timeout=30)
        started = time.perf_counter()
        api.call("/Sessions?search=" + run, opener=client[0])
        return (time.perf_counter() - started) * 1000

    print("Measuring 50 concurrent Web session lists...", flush=True)
    with ThreadPoolExecutor(max_workers=count) as pool:
        list_ms = list(pool.map(list_sessions, clients))
    def summary(values):
        ordered = sorted(values)
        return {"count": len(values), "p50Ms": round(ordered[len(values)//2], 2),
                "p95Ms": round(ordered[math.ceil(len(values)*0.95)-1], 2), "maxMs": round(max(values), 2)}
    result = {"run": run, "sessionId": sid, "students": count, "synchronizedStart": True, "transport": "HTTP loopback, same Windows host",
        "measurement": "client end-to-end wall time; includes network and HTML serialization",
        "saves": summary(save_ms), "lists": summary(list_ms),
        "saveTargetMs": 2000, "listTargetMs": 3000}
    result["targetsMet"] = result["saves"]["p95Ms"] <= 2000 and result["lists"]["p95Ms"] <= 3000
    output = api.ROOT / "artifacts/integration"
    output.mkdir(parents=True, exist_ok=True)
    (output / "performance.json").write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(result, ensure_ascii=False, indent=2), flush=True)
    # End load attempts using the same service path; leave their history available for audit.
    with ThreadPoolExecutor(max_workers=8) as pool:
        list(pool.map(lambda c: api.call(f"/Attempts/Submit/{c[1]}", "POST", {}, opener=c[0], csrf=c[2]), clients))
    api.call("/api/auth/logout", "POST", {}, admin, expected=(204,))
    api.call("/api/auth/logout", "POST", {}, officer, expected=(204,))


if __name__ == "__main__":
    main()
