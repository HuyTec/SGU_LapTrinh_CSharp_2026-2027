# Desktop verification

Code-generated offscreen WinForms rendering and real API-client checks on Windows. This does not establish interactive OS usability. Actual footer controls are rendered separately and composited at their native bounds to work around intermittent WinForms DrawToBitmap child-paint omission.

- PASS: Rendered 01-login.png (600×520).
- PASS: admin API login/catalog returned 2 authorized resources.
- Pagination label: Trang 1 / 1 — 13 mục, bounds={X=18,Y=60,Width=176,Height=23}
- PASS: Rendered 02-main-admin.png (1200×800).
- PASS: admin users list/detail real API returned data.
- Layout 03-editor-users.png: footer {X=0,Y=682,Width=862,Height=71}, visible=True; buttons=Hủy:{X=752,Y=19,Width=75,Height=33}:True;Lưu thay đổi:{X=631,Y=19,Width=115,Height=33}:True
- PASS: Rendered 03-editor-users.png (880×800).
- PASS: Rendered 04-import.png (960×640).
- PASS: lecturer API login/catalog returned 4 authorized resources.
- Pagination label: Trang 1 / 1 — 3 mục, bounds={X=18,Y=60,Width=167,Height=23}
- PASS: Rendered 02-main-lecturer.png (1200×800).
- PASS: lecturer questions list/detail real API returned data.
- Layout 03-editor-questions.png: footer {X=0,Y=682,Width=862,Height=71}, visible=True; buttons=Hủy:{X=752,Y=19,Width=75,Height=33}:True;Lưu thay đổi:{X=631,Y=19,Width=115,Height=33}:True
- PASS: Rendered 03-editor-questions.png (880×800).
- PASS: lecturer exams list/detail real API returned data.
- Layout 03-editor-exams.png: footer {X=0,Y=682,Width=862,Height=71}, visible=True; buttons=Hủy:{X=752,Y=19,Width=75,Height=33}:True;Lưu thay đổi:{X=631,Y=19,Width=115,Height=33}:True
- PASS: Rendered 03-editor-exams.png (880×800).
- PASS: Exam editor uses question selection/individual points grid, not a JSON editor.
- PASS: Actual question Form saved option text and one correct radio through the API; isolated question deleted.
- PASS: Actual exam Form saved 3.33 + 3.33 + 3.34 individual points to SQL; isolated draft exam deleted.
- PASS: officer API login/catalog returned 4 authorized resources.
- Pagination label: Trang 1 / 1 — 4 mục, bounds={X=18,Y=60,Width=167,Height=23}
- PASS: Rendered 02-main-officer.png (1200×800).
- PASS: officer sessions list/detail real API returned data.
- Layout 03-editor-sessions.png: footer {X=0,Y=682,Width=862,Height=71}, visible=True; buttons=Hủy:{X=752,Y=19,Width=75,Height=33}:True;Lưu thay đổi:{X=631,Y=19,Width=115,Height=33}:True
- PASS: Rendered 03-editor-sessions.png (880×800).
- PASS: Officer real Desktop API-client create/read/update/delete persisted and cleaned up an isolated subject.
- PASS: Offscreen forms constructed/rendered without unhandled exceptions.

Visual review corrected CSV instruction clipping, logout contrast, and restricted translated enum labels so literal usernames/student codes remain unchanged.

## Hidden concurrency version follow-up

- PASS: Actual existing question Form preserved the original hidden version through API save; SQL edit advanced the version.
- PASS: Both new/edit Form input dictionaries omit the hidden Base64 version control.
- PASS: Isolated question used for concurrency round-trip verification was deleted; seed questions untouched.
- PASS: Offscreen forms constructed without unhandled exceptions.

## In-app guidance follow-up

- PASS: Rendered 01-login.png (600×620).
- PASS: Login form exposes guidance and student Web-registration buttons at 600×620.
- Layout 05-help-login.png: footer {X=0,Y=502,Width=802,Height=71}, visible=True; buttons=Đóng hướng dẫn:{X=616,Y=19,Width=151,Height=33}:True
- PASS: Rendered 05-help-login.png (820×620).
- PASS: Rendered scrollable guidance for login, including student Web registration and administrator-issued personnel roles.
- Layout 05-help-Administrator.png: footer {X=0,Y=502,Width=802,Height=71}, visible=True; buttons=Đóng hướng dẫn:{X=616,Y=19,Width=151,Height=33}:True
- PASS: Rendered 05-help-Administrator.png (820×620).
- PASS: Rendered scrollable guidance for Administrator.
- Layout 05-help-Lecturer.png: footer {X=0,Y=502,Width=802,Height=71}, visible=True; buttons=Đóng hướng dẫn:{X=616,Y=19,Width=151,Height=33}:True
- PASS: Rendered 05-help-Lecturer.png (820×620).
- PASS: Rendered scrollable guidance for Lecturer.
- Layout 05-help-ExamOfficer.png: footer {X=0,Y=502,Width=802,Height=71}, visible=True; buttons=Đóng hướng dẫn:{X=616,Y=19,Width=151,Height=33}:True
- PASS: Rendered 05-help-ExamOfficer.png (820×620).
- PASS: Rendered scrollable guidance for ExamOfficer.
- Browser launch was not invoked during offscreen checks; registration target HTTP(S) validation and /Account/Register construction were inspected in source.
- PASS: Offscreen forms constructed without unhandled exceptions.

## In-app guidance follow-up

- PASS: Rendered 01-login.png (600×620).
- PASS: Login form exposes guidance and all-role Web-registration buttons at 600×620.
- Layout 05-help-login.png: footer {X=0,Y=502,Width=802,Height=71}, visible=True; buttons=Đóng hướng dẫn:{X=616,Y=19,Width=151,Height=33}:True
- PASS: Rendered 05-help-login.png (820×620).
- PASS: Rendered scrollable guidance for login, including all-role Web registration and pending personnel approval.
- Layout 05-help-Administrator.png: footer {X=0,Y=502,Width=802,Height=71}, visible=True; buttons=Đóng hướng dẫn:{X=616,Y=19,Width=151,Height=33}:True
- PASS: Rendered 05-help-Administrator.png (820×620).
- PASS: Rendered scrollable guidance for Administrator.
- Layout 05-help-Lecturer.png: footer {X=0,Y=502,Width=802,Height=71}, visible=True; buttons=Đóng hướng dẫn:{X=616,Y=19,Width=151,Height=33}:True
- PASS: Rendered 05-help-Lecturer.png (820×620).
- PASS: Rendered scrollable guidance for Lecturer.
- Layout 05-help-ExamOfficer.png: footer {X=0,Y=502,Width=802,Height=71}, visible=True; buttons=Đóng hướng dẫn:{X=616,Y=19,Width=151,Height=33}:True
- PASS: Rendered 05-help-ExamOfficer.png (820×620).
- PASS: Rendered scrollable guidance for ExamOfficer.
- Browser launch was not invoked during offscreen checks; registration target HTTP(S) validation and /Account/Register construction were inspected in source.
- PASS: Offscreen forms constructed without unhandled exceptions.

## Staff registration approval follow-up

- Pagination label: Trang 1 / 6 — 140 mục, bounds={X=18,Y=60,Width=185,Height=23}
- PASS: Rendered 06-admin-approval-menu.png (1200×800).
- Pagination label: Trang 1 / 1 — 1 mục, bounds={X=261,Y=60,Width=167,Height=23}
- PASS: Rendered 06-pending-staff.png (1000×700).
- PASS: Pending Lecturer row is visible with Vietnamese requested-role header/value and an accessible approval button at 1000×700.
- PASS: Desktop API-client approved only the browser-created Lecturer fixture; personnel login/catalog succeeded afterward.
- Fixture staff_lecturer_mvmareao remains approved for the browser QA handoff. Existing seed/user accounts were not altered. Native confirmation-dialog interaction was not automated.
- PASS: Offscreen forms constructed without unhandled exceptions.

## Fresh topic lookup regression

- Pagination label: Trang 1 / 1 — 6 mục, bounds={X=18,Y=60,Width=167,Height=23}
- PASS: Rendered 07-topic-lookup-initial.png (1200×800).
- PASS: Actual question navigation refreshed lookup choices for a topic created after MainForm initialization.
- PASS: Actual Làm mới button fetched a second new topic and preserved selected subject/topic/difficulty filters.
- PASS: Switching Topics → Questions retained fresh lookups and reset screen filters.
- PASS: Lecturer2 catalog and topics API could not see either topic in the unassigned subject.
- Pagination label: Trang 1 / 1 — 17 mục, bounds={X=539,Y=64,Width=176,Height=23}
- PASS: Rendered 07-topic-lookup-refreshed.png (1200×800).
- PASS: Only isolated QA topics were created/deleted; existing topics including Thiên văn were not modified.
- PASS: Offscreen forms constructed without unhandled exceptions.
