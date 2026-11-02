"""HTTP acceptance on a running local demo. Creates its own uniquely named data."""
from pathlib import Path
from html.parser import HTMLParser
from html import unescape
from urllib import request, error, parse
from http.cookiejar import CookieJar
from datetime import datetime, timezone, timedelta
import csv
import io
import json
import re
import sys
import time
import uuid
import zipfile

ROOT = Path(__file__).resolve().parents[1]
BASE = sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5180"
RUN = uuid.uuid4().hex[:10]
PASSWORD = next(x[9:] for x in (ROOT / ".local/demo-credentials.txt").read_text(encoding="utf-8-sig").splitlines() if x.startswith("password="))
CHECKS = []


def check(name, condition=True):
    if not condition:
        raise AssertionError(name)
    CHECKS.append(name)
    print("PASS " + name, flush=True)


def call(path, method="GET", body=None, token=None, opener=None, form=None, expected=(200,), csrf=None):
    headers = {"Accept": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    if csrf:
        headers["RequestVerificationToken"] = csrf
    data = None
    if body is not None:
        headers["Content-Type"] = "application/json"
        data = json.dumps(body).encode()
    if form is not None:
        headers["Content-Type"] = "application/x-www-form-urlencoded"
        data = parse.urlencode(form).encode()
    req = request.Request(BASE + path, data=data, headers=headers, method=method)
    try:
        response = (opener or request.build_opener()).open(req, timeout=40)
    except error.HTTPError as exc:
        response = exc
    raw = response.read()
    text = raw.decode("utf-8-sig", errors="replace")
    assert response.code in expected, f"{method} {path}: HTTP {response.code}: {text[:450]}"
    content = json.loads(text) if "json" in response.headers.get("Content-Type", "") and text else text
    return content, response.geturl(), raw


def login_staff(name, role=None):
    result, _, _ = call("/api/auth/login", "POST", {"userName": name, "password": PASSWORD, "role": role})
    return result["token"]


def save(token, resource, values, identity=None):
    route = f"/api/admin/{resource}" + (f"/{identity}" if identity is not None else "")
    result, _, _ = call(route, "PUT" if identity is not None else "POST", {"values": values}, token)
    return result["id"]


def action(token, resource, identity, name, expected=(204,)):
    return call(f"/api/admin/{resource}/{identity}/action", "POST", {"action": name}, token, expected=expected)


class Inputs(HTMLParser):
    def __init__(self, text):
        super().__init__()
        self.token = ""
        self.questions = {}
        self.qid = None
        self.label = None
        self.feed(text)

    def handle_starttag(self, tag, attrs):
        a = dict(attrs)
        if tag == "fieldset" and a.get("data-question-id"):
            self.qid = int(a["data-question-id"])
            self.questions[self.qid] = []
        if tag == "label":
            self.label = {"value": None, "text": ""}
        if tag == "input" and a.get("name") == "__RequestVerificationToken":
            self.token = a["value"]
        if tag == "input" and a.get("type") == "radio" and self.label is not None:
            self.label["value"] = int(a["value"])

    def handle_data(self, text):
        if self.label is not None:
            self.label["text"] += text

    def handle_endtag(self, tag):
        if tag == "label" and self.label is not None:
            if self.qid is not None and self.label["value"] is not None:
                self.questions[self.qid].append(self.label)
            self.label = None
        if tag == "fieldset":
            self.qid = None


def login_student(name):
    browser = request.build_opener(request.HTTPCookieProcessor(CookieJar()))
    page, _, _ = call("/Account/Login", opener=browser)
    token = Inputs(page).token
    _, final_url, _ = call("/Account/Login", "POST", opener=browser,
                          form={"UserName": name, "Password": PASSWORD, "__RequestVerificationToken": token})
    assert "/Sessions" in final_url, "Student login did not open sessions"
    return browser


def main():
    call("/api/admin/catalog", expected=(401,))
    check("Unauthenticated API rejected")
    call("/api/auth/login", "POST", {"userName": "admin", "password": "incorrect-password"}, expected=(401,))
    check("Incorrect credentials are rejected without creating a session")
    tokens = {name: login_staff(name) for name in ("admin", "officer", "officer2", "lecturer", "lecturer2")}
    call("/api/auth/login", "POST", {"userName": "student", "password": PASSWORD}, expected=(403,))
    check("Student cannot log into the Desktop API")
    users, _, _ = call("/api/admin/users?search=lecturer&pageSize=200", token=tokens["admin"])
    lecturer_id = next(u["id"] for u in users["items"] if u["userName"] == "lecturer")
    student_name = "e2e_" + RUN
    other_name = "other_" + RUN
    student = save(tokens["admin"], "users", {"userName": student_name, "fullName": "Thí sinh HTTP " + RUN,
                   "studentCode": "E" + RUN, "password": PASSWORD, "roles": ["Student"], "isActive": True})
    other_student = save(tokens["admin"], "users", {"userName": other_name, "fullName": "Thí sinh khác " + RUN,
         "studentCode": "O" + RUN, "password": PASSWORD, "roles": ["Student"], "isActive": True})
    subject = save(tokens["officer"], "subjects", {"code": "E2E" + RUN, "name": "Môn HTTP " + RUN, "isActive": True})
    assignment = save(tokens["admin"], "assignments", {"lecturerId": lecturer_id, "subjectId": subject})
    topic = save(tokens["lecturer"], "topics", {"subjectId": subject, "name": "Chủ đề HTTP", "isActive": True})
    call("/api/admin/topics", "POST", {"values": {"subjectId": subject, "name": "Ngoài quyền", "isActive": True}},
         tokens["lecturer2"], expected=(403,))
    check("Lecturer cannot write another lecturer's subject")
    question_ids = []
    extra_values = {"topicId": topic, "text": "Kiểm tra phiên bản", "difficulty": "Easy", "option1": "A", "option2": "B", "option3": "C", "option4": "D", "correctIndex": 1, "isActive": True}
    extra = save(tokens["lecturer"], "questions", extra_values)
    original, _, _ = call(f"/api/admin/questions/{extra}", token=tokens["lecturer"])
    save(tokens["lecturer"], "questions", {**extra_values, "correctIndex": 3, "version": original["version"]}, extra)
    updated, _, _ = call(f"/api/admin/questions/{extra}", token=tokens["lecturer"])
    check("Option-only edit advances question version", updated["correctIndex"] == 3 and updated["version"] != original["version"])
    for stale in (original["version"], "not-base64", ""):
        call(f"/api/admin/questions/{extra}", "PUT", {"values": {**extra_values, "version": stale}}, tokens["lecturer"], expected=(409,))
    check("Stale, missing and malformed question versions cannot overwrite newer data")
    call(f"/api/admin/questions/{extra}", "DELETE", token=tokens["lecturer"], expected=(204,))
    call(f"/api/admin/questions/{extra}", token=tokens["lecturer"], expected=(404,))
    check("Unused question can be permanently deleted")
    for i in range(3):
        question_ids.append(save(tokens["lecturer"], "questions", {"topicId": topic, "text": f"Câu {i+1} <script>window.bad=1</script>",
            "difficulty": "Medium", "option1": "Đúng", "option2": "Sai B", "option3": "Sai C", "option4": "Sai D", "correctIndex": 1, "isActive": True}))
    another_subject = save(tokens["officer"], "subjects", {"code": "ALT" + RUN, "name": "Môn khác " + RUN, "isActive": True})
    save(tokens["admin"], "assignments", {"lecturerId": lecturer_id, "subjectId": another_subject})
    call(f"/api/admin/topics/{topic}", "PUT", {"values": {"subjectId": another_subject, "name": "Chuyển môn", "isActive": True}}, tokens["lecturer"], expected=(409,))
    call(f"/api/admin/topics/{topic}", "DELETE", token=tokens["lecturer"], expected=(400, 409))
    check("Topic with questions cannot change subject or be deleted")
    exam = save(tokens["lecturer"], "exams", {"subjectId": subject, "title": "Đề HTTP " + RUN,
        "questionIds": question_ids, "questionPoints": {str(q): p for q, p in zip(question_ids, [3, 3, 4])}})
    action(tokens["lecturer"], "exams", exam, "ready")
    check("Three-question exam with individual points 3+3+4 becomes Ready")
    now = datetime.now(timezone.utc)
    closes = now + timedelta(seconds=30)
    session_values = {"examId": exam, "title": "Đợt HTTP " + RUN, "opensAt": (now - timedelta(minutes=1)).isoformat(),
        "closesAt": closes.isoformat(), "durationMinutes": 5, "shuffleQuestions": True, "shuffleOptions": True, "reviewPolicy": "CorrectAnswers"}
    session = save(tokens["officer"], "sessions", session_values)
    action(tokens["officer"], "sessions", session, "activate", expected=(400,))
    save(tokens["officer"], "candidates", {"sessionId": session, "studentId": student})
    call("/api/admin/candidates", "POST", {"values": {"sessionId": session, "studentId": student}}, tokens["officer"], expected=(400,))
    duplicate_session = save(tokens["officer"], "sessions", {**session_values, "title": "Trùng lịch " + RUN})
    action(tokens["lecturer"], "exams", exam, "draft")
    action(tokens["lecturer"], "exams", exam, "ready")
    check("Exam can return to draft while its associated sessions have never opened")
    call("/api/admin/candidates", "POST", {"values": {"sessionId": duplicate_session, "studentId": student}}, tokens["officer"], expected=(400,))
    check("Assignment rejects duplicate and overlapping sessions")
    save(tokens["officer"], "candidates", {"sessionId": session, "studentId": other_student})
    action(tokens["officer"], "sessions", session, "activate")
    action(tokens["lecturer"], "exams", exam, "draft", expected=(400, 409))
    action(tokens["officer2"], "sessions", session, "publish", expected=(403,))
    call(f"/api/admin/sessions/{session}", "PUT", {"values": session_values}, tokens["officer"], expected=(400,))
    check("Activated session settings are locked and another officer is denied")
    browser = login_student(student_name)
    other = login_student(other_name)
    listing, _, _ = call("/Sessions?search=" + RUN, opener=browser)
    check("Desktop API changes are visible on the Web through the same SQL data", "Đợt HTTP " + RUN in unescape(listing))
    details, _, _ = call(f"/Sessions/Details/{session}", opener=browser)
    page, final_url, _ = call(f"/Sessions/Start/{session}", "POST", opener=browser,
                            form={"__RequestVerificationToken": Inputs(details).token})
    attempt_id = int(re.search(r"/Attempts/Take/(\d+)", final_url)[1])
    parsed = Inputs(page)
    check("Attempt includes three questions and no correct-answer fields", len(parsed.questions) == 3 and "IsCorrect" not in page and "CorrectAnswer" not in page)
    check("Question text is HTML encoded", "&lt;script&gt;" in page and "<script>window.bad" not in page)
    _, resumed_url, _ = call(f"/Sessions/Start/{session}", "POST", opener=browser,
                            form={"__RequestVerificationToken": parsed.token})
    check("Starting again resumes the same attempt", resumed_url.endswith("/" + str(attempt_id)))
    call(f"/Attempts/Take/{attempt_id}", opener=other, expected=(403,))
    check("Cross-student access returns 403")
    call(f"/Attempts/Save/{attempt_id}", "POST", {}, opener=browser, expected=(400,))
    check("Cookie mutation without antiforgery is rejected")
    for qid, choices in parsed.questions.items():
        correct = next(o["value"] for o in choices if o["text"].strip() == "Đúng")
        call(f"/Attempts/Save/{attempt_id}", "POST", {"questionId": qid, "optionId": correct, "isFlagged": True},
             opener=browser, csrf=parsed.token)
    call(f"/Attempts/Submit/{attempt_id}", "POST", {}, opener=browser, csrf=parsed.token)
    call(f"/Attempts/Submit/{attempt_id}", "POST", {}, opener=browser, csrf=parsed.token)
    unpublished, _, _ = call(f"/Attempts/Result/{attempt_id}", opener=browser)
    check("Repeated submission succeeds and unpublished score stays hidden", "Kết quả chưa được công bố" in unescape(unpublished) and 'class="result-score"' not in unpublished)
    before, _, _ = call("/api/admin/monitor?pageSize=200", token=tokens["officer"])
    not_started = next(r for r in before["items"] if r["sessionId"] == session and r["studentCode"] == "O" + RUN)
    check("Monitoring distinguishes not-started students from a zero score", not_started["status"] == "NotStarted" and not_started["score"] is None)
    other_details, _, _ = call(f"/Sessions/Details/{session}", opener=other)
    call(f"/Sessions/Start/{session}", "POST", opener=other, form={"__RequestVerificationToken": Inputs(other_details).token})
    action(tokens["officer"], "sessions", session, "publish", expected=(400,))
    # Wait for this short-lived test session to close, without altering a real exam schedule.
    while datetime.now(timezone.utc) <= closes + timedelta(milliseconds=500):
        time.sleep(0.5)
    # Monitor only reads state; it does not call ExpireAsync. The hosted worker must grade it.
    deadline = time.monotonic() + 20
    while True:
        monitor, _, _ = call("/api/admin/monitor?pageSize=200", token=tokens["officer"])
        timed_out = next(r for r in monitor["items"] if r["sessionId"] == session and r["studentCode"] == "O" + RUN)
        if timed_out["status"] == "Submitted":
            break
        assert time.monotonic() < deadline, "Hosted expiry worker did not finish the unanswered attempt"
        time.sleep(0.5)
    check("Hosted worker grades an abandoned attempt without a browser request", timed_out["score"] == 0)
    action(tokens["officer"], "sessions", session, "publish")
    published, _, _ = call(f"/Attempts/Result/{attempt_id}", opener=browser)
    check("Desktop publication makes score 10 and permitted review visible on Web", 'class="result-score"' in published and "Đáp án đúng:" in unescape(published) and re.search(r'class="result-score">10\s', published))
    response, _, csv_bytes = call(f"/api/admin/sessions/{session}/export?format=csv", token=tokens["officer"])
    csv_rows = list(csv.reader(io.StringIO(csv_bytes.decode("utf-8-sig"))))
    mine = next(r for r in csv_rows[1:] if r[1] == "E" + RUN)
    check("CSV contains the assigned student's score", len(csv_rows) == 3 and float(mine[4]) == 10)
    _, _, xlsx = call(f"/api/admin/sessions/{session}/export?format=xlsx", token=tokens["officer"])
    with zipfile.ZipFile(io.BytesIO(xlsx)) as z:
        check("XLSX is a valid OpenXML workbook with result data", "xl/workbook.xml" in z.namelist() and "10" in z.read("xl/worksheets/sheet1.xml").decode())
    call(f"/api/admin/sessions/{session}/export?format=csv", token=tokens["officer2"], expected=(403,))
    call("/api/admin/questions", token=tokens["admin"], expected=(403,))
    check("Export scope and account-admin separation are enforced")
    future = datetime.now(timezone.utc) + timedelta(minutes=10)
    future_values = {**session_values, "title": "Đợt hoãn " + RUN, "opensAt": future.isoformat(), "closesAt": (future + timedelta(minutes=2)).isoformat(), "durationMinutes": 1}
    future_id = save(tokens["officer"], "sessions", future_values)
    save(tokens["officer"], "candidates", {"sessionId": future_id, "studentId": student})
    action(tokens["officer"], "sessions", future_id, "activate")
    action(tokens["officer"], "sessions", future_id, "postpone")
    postponed, _, _ = call(f"/api/admin/sessions/{future_id}", token=tokens["officer"])
    check("Postponing a future session returns it to Draft", postponed["status"] == "Draft")
    save(tokens["officer"], "sessions", future_values, future_id)
    action(tokens["officer"], "sessions", future_id, "activate")
    action(tokens["officer"], "sessions", future_id, "cancel")
    cancelled, _, _ = call(f"/api/admin/sessions/{future_id}", token=tokens["officer"])
    check("A rescheduled session can be activated then cancelled before opening", cancelled["status"] == "Cancelled")
    call(f"/api/admin/questions/{question_ids[0]}", "DELETE", token=tokens["lecturer"], expected=(204,))
    inactive, _, _ = call(f"/api/admin/questions/{question_ids[0]}", token=tokens["lecturer"])
    preserved, _, _ = call(f"/Attempts/Result/{attempt_id}", opener=browser)
    check("Used question is retired while the published snapshot score is preserved", inactive["isActive"] is False and re.search(r'class="result-score">10\s', preserved))
    call(f"/api/admin/assignments/{assignment}", "DELETE", token=tokens["admin"], expected=(204,))
    call("/api/admin/topics", "POST", {"values": {"subjectId": subject, "name": "Đã gỡ quyền", "isActive": True}}, tokens["lecturer"], expected=(403,))
    check("Removing a lecturer assignment immediately removes subject write access")
    # CSV import validates the entire batch before writing and supports preview.
    import_name = "import_" + RUN
    good_csv = "UserName,FullName,StudentCode,Email,Password\r\n" + f'{import_name},"Tên, có dấu phẩy",I{RUN},,{PASSWORD}\r\n'
    call("/api/admin/users/import", "POST", {"csv": good_csv, "preview": True}, tokens["admin"])
    empty, _, _ = call("/api/admin/users?search=" + import_name, token=tokens["admin"])
    check("Import preview does not write users", empty["total"] == 0)
    bad_csv = good_csv + f'{import_name}_bad,Thiếu mật khẩu,J{RUN},,x\r\n'
    call("/api/admin/users/import", "POST", {"csv": bad_csv}, tokens["admin"], expected=(400,))
    empty, _, _ = call("/api/admin/users?search=" + import_name, token=tokens["admin"])
    check("Invalid CSV batch is all-or-nothing", empty["total"] == 0)
    imported, _, _ = call("/api/admin/users/import", "POST", {"csv": good_csv}, tokens["admin"])
    check("Quoted CSV import creates the valid student", imported["imported"] == 1)
    multi_name = "multi_" + RUN
    multi_id = save(tokens["admin"], "users", {"userName": multi_name, "fullName": "Nhiều vai trò", "password": PASSWORD,
         "roles": ["Lecturer", "ExamOfficer"], "isActive": True})
    multi_teacher = login_staff(multi_name, "Lecturer")
    multi_officer = login_staff(multi_name, "ExamOfficer")
    teacher_catalog, _, _ = call("/api/admin/catalog", token=multi_teacher)
    officer_catalog, _, _ = call("/api/admin/catalog", token=multi_officer)
    check("One Identity user can select distinct operational roles", teacher_catalog["role"] == "Lecturer" and officer_catalog["role"] == "ExamOfficer")
    action(tokens["admin"], "users", multi_id, "lock")
    call("/api/admin/catalog", token=multi_teacher, expected=(401,))
    call("/api/admin/catalog", token=multi_officer, expected=(401,))
    check("Account lock revokes both active JWT sessions")
    call("/api/auth/login", "POST", {"userName": multi_name, "password": PASSWORD}, expected=(401,))
    check("Locked accounts cannot create a new session")
    action(tokens["admin"], "users", multi_id, "unlock")
    reset_password = PASSWORD + "B2!"
    save(tokens["admin"], "users", {"userName": multi_name, "fullName": "Nhiều vai trò", "password": reset_password,
         "roles": ["Lecturer", "ExamOfficer"], "isActive": True}, multi_id)
    call("/api/auth/login", "POST", {"userName": multi_name, "password": PASSWORD}, expected=(401,))
    reset_login, _, _ = call("/api/auth/login", "POST", {"userName": multi_name, "password": reset_password, "role": "Lecturer"})
    call("/api/auth/logout", "POST", {}, reset_login["token"], expected=(204,))
    check("Admin unlock and password reset replace the old password")
    logout_token = login_staff("lecturer2")
    call("/api/auth/logout", "POST", {}, logout_token, expected=(204,))
    call("/api/admin/catalog", token=logout_token, expected=(401,))
    check("JWT logout invalidates the old token on the server")
    output = ROOT / "artifacts/integration"
    output.mkdir(parents=True, exist_ok=True)
    (output / "http-results.json").write_text(json.dumps({"run": RUN, "sessionId": session, "attemptId": attempt_id,
        "passed": len(CHECKS), "checks": CHECKS}, ensure_ascii=False, indent=2), encoding="utf-8")
    (output / "results.csv").write_bytes(csv_bytes)
    (output / "results.xlsx").write_bytes(xlsx)
    print(f"HTTP acceptance: {len(CHECKS)} checks passed", flush=True)


if __name__ == "__main__":
    main()
