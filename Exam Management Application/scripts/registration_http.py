"""Focused registration acceptance against the running local host; creates unique students."""
from html import unescape
from http.cookiejar import CookieJar
from urllib import request
import json
import uuid

import smoke_http as http


def main():
    browser = request.build_opener(request.HTTPCookieProcessor(CookieJar()))
    suffix = uuid.uuid4().hex[:10]
    name = "register_" + suffix
    base = {"UserName": name, "FullName": "Thí sinh đăng ký " + suffix,
            "StudentCode": "REG" + suffix, "Email": name + "@example.com",
            "Password": http.PASSWORD, "ConfirmPassword": http.PASSWORD}
    checks = []

    def check(label, ok):
        assert ok, label
        checks.append(label)
        print("PASS " + label, flush=True)

    def submit(values, expected=(200,)):
        page, _, _ = http.call("/Account/Register", opener=browser)
        return http.call("/Account/Register", "POST", opener=browser,
                         form={**values, "__RequestVerificationToken": http.Inputs(page).token}, expected=expected)

    guide, _, _ = http.call("/Help")
    check("Public guide explains registration and assigned sessions", "Đăng ký" in unescape(guide) and "phân công" in unescape(guide))
    http.call("/Account/Register", "POST", opener=browser, form=base, expected=(400,))
    check("Registration requires antiforgery", True)
    bad, _, _ = submit({**base, "ConfirmPassword": "mismatch"})
    check("Confirmation mismatch is visible and passwords are not echoed", "không khớp" in unescape(bad) and http.PASSWORD not in bad)
    missing, _, _ = submit({**base, "StudentCode": ""})
    bad_email, _, _ = submit({**base, "Email": "invalid-email"})
    check("Student code and valid email are required", "Vui lòng nhập mã thí sinh" in unescape(missing) and "không đúng định dạng" in unescape(bad_email))
    weak, _, _ = submit({**base, "Password": "password123", "ConfirmPassword": "password123"})
    check("Identity rejects weak passwords", "Mật khẩu cần" in unescape(weak))
    admin = http.login_staff("admin")
    empty, _, _ = http.call("/api/admin/users?search=" + name, token=admin)
    check("Invalid registration never creates a partial account", empty["total"] == 0)
    success, url, _ = submit({**base, "Role": "Administrator", "Roles": "Administrator"})
    check("Successful registration redirects to login with guidance", "/Account/Login" in url and "Đăng ký thành công" in unescape(success))
    users, _, _ = http.call("/api/admin/users?search=" + name, token=admin)
    user = next(u for u in users["items"] if u["userName"] == name)
    check("Registered fields persist and public role injection cannot grant staff access",
          user["fullName"] == base["FullName"] and user["studentCode"] == base["StudentCode"] and user["email"] == base["Email"]
          and user["roles"] == ["Student"])
    http.call("/api/auth/login", "POST", {"userName": name, "password": http.PASSWORD, "role": "Administrator"}, expected=(403,))
    check("Registered student cannot authenticate as personnel", True)
    student = http.login_student(name)
    sessions, _, _ = http.call("/Sessions", opener=student)
    check("Registered student can log in and has no automatically assigned session", "Chưa có đợt thi" in unescape(sessions))
    duplicate, _, _ = submit(base)
    check("Duplicate username is reported", "Tên đăng nhập đã được sử dụng" in unescape(duplicate))
    duplicate_code, _, _ = submit({**base, "UserName": name + "_other"})
    check("Duplicate student code is reported", "Mã thí sinh đã được sử dụng" in unescape(duplicate_code))
    invalid_type, _, _ = submit({**base, "UserName": name + "_invalid", "AccountType": "SuperAdmin"})
    check("Unknown account types are rejected", "không hợp lệ" in unescape(invalid_type))
    officer = http.login_staff("officer")
    for role in ("Lecturer", "ExamOfficer", "Administrator"):
        staff_name = name + "_" + role.lower()
        pending, pending_url, _ = submit({**base, "UserName": staff_name, "AccountType": role, "StudentCode": ""})
        check(role + " registration accepts no student code and shows pending approval", "/Account/Login" in pending_url and "duyệt" in unescape(pending))
        listing, _, _ = http.call("/api/admin/users?search=" + staff_name, token=admin)
        staff = next(u for u in listing["items"] if u["userName"] == staff_name)
        check(role + " request has no usable privileges before approval", staff["isActive"] is False and staff["roles"] == [] and staff["requestedRole"] == role)
        rejected, _, _ = http.call("/api/auth/login", "POST", {"userName": staff_name, "password": http.PASSWORD}, expected=(403,))
        check(role + " pending login explains approval status", "duyệt" in rejected.get("detail", ""))
        http.call("/api/auth/login", "POST", {"userName": staff_name, "password": "wrong-password"}, expected=(401,))
        http.action(officer, "users", staff["id"], "approve", expected=(403,))
        http.action(admin, "users", staff["id"], "unlock", expected=(400, 409))
        check(role + " approval cannot be bypassed by non-admin or unlock", True)
        http.action(admin, "users", staff["id"], "approve")
        approved, _, _ = http.call(f"/api/admin/users/{staff['id']}", token=admin)
        check(role + " approval activates exactly the requested role", approved["isActive"] is True and approved["roles"] == [role] and approved["requestedRole"] is None)
        login, _, _ = http.call("/api/auth/login", "POST", {"userName": staff_name, "password": http.PASSWORD, "role": role})
        check(role + " approved account can log into Desktop", login["role"] == role)
        http.call("/api/auth/logout", "POST", {}, login["token"], expected=(204,))
        http.action(admin, "users", staff["id"], "approve", expected=(409,))
        check(role + " approval cannot be applied twice", True)
    http.call("/api/auth/logout", "POST", {}, officer, expected=(204,))
    http.call("/api/auth/logout", "POST", {}, admin, expected=(204,))
    output = http.ROOT / "artifacts/integration/registration-results.json"
    output.write_text(json.dumps({"userName": name, "passed": len(checks), "checks": checks}, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Registration acceptance: {len(checks)} checks passed")


if __name__ == "__main__":
    main()
