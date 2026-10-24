# Web UI verification — 24/10/2026

Target: `http://localhost:5180`, Microsoft Edge headless via Playwright, SQL Server demo data. All passwords were read from ignored local configuration and were never printed or embedded in screenshots/scripts.

## Observed checks

| Check | Actual outcome |
|---|---|
| Login, sessions, details, taking exam, result, history, home | Rendered; screenshots captured at 360 px and 1440 px |
| Mobile horizontal overflow | All checked pages: viewport width 360, document scroll width 360 |
| Empty search | No-match state and clear-filter action visible |
| Save answer and flag | Server confirmed save; reload restored both values |
| Save queue | Delayed first save by 500 ms, changed the same question twice, immediately submitted: two saves completed and result navigation succeeded |
| Unpublished result | No score or answer review displayed |
| Published result | Integration attempt 3: score 10/10, 3/3 correct, three permitted correct-answer review items; history shows 10/10 |
| Published result mobile | Viewport width 360, scroll width 360; score and review visually inspected |
| Browser failures during normal flow | Zero page errors and zero HTTP responses >=400 |
| Keyboard login | Tab moved username → password → submit; Enter submitted login |
| Keyboard radio | Space selected an answer |
| Network failure | Aborted save showed visible failure text and retry button; retry succeeded and reload restored selection |
| Submit dialog | Initial focus on cancel; Escape closed dialog without submitting |

`student2` completed demo attempt 1. `student` demo attempt 2 was left in progress after save/reload/resilience checks. No administrator actions or publication changes were performed.

## Visual review

Inspected rendered screenshots of mobile login, details, take, unpublished result, desktop sessions and history. Also inspected published result at 1440 px and 360 px and mobile published history: score 10/10 and 3/3 correct were readable, all three review items displayed the selected/correct answers, and script-shaped question text remained visible as encoded text. Text and control labels were readable; controls and question options fit their containers; no overlapping elements were observed. Navigation wraps at 360 px. Long question content uses automatic Razor encoding and pre-line wrapping. The take screenshot is intentionally full-page, so the question navigator appears after the questions on mobile.

## Evidence and boundaries

- `qa-results.json`: initial end-to-end observations.
- `resilience-results.json`: keyboard, network retry and dialog observations.
- `published-results.json`: published score, review count and history checks; [desktop screenshot](result-published-desktop.png), [mobile screenshot](result-published-mobile.png), [history screenshot](history-published-mobile.png).
- PNGs in this folder: actual browser captures, no mockups.
- Scripts: local reproducible Playwright checks; run them only against disposable demo data because they create/finish student attempts.
- Root is separately testing publication/authorization/server expiry and Desktop → API → SQL Server → Web integration.
- Initial browser capture used host timezone; scripts now explicitly request `Asia/Ho_Chi_Minh`. Re-ran resilience checks against the restarted latest build: browser resolved timezone to equivalent `Asia/Saigon`; rendered deadline was `2026-10-24T08:13:27.2616874Z`; clock showed `24:06` remaining (no false seven-hour expiry). Keyboard, network failure/retry, restored selection, cancel focus and Escape all passed again. The Web parser appends `Z` defensively to timezone-less dates and Razor UTC attributes now include `Z` explicitly.
- No claim is made for screen-reader usability, load performance, or real simultaneous-user capacity from these UI checks.
- Final structural change: Result and History explicitly show `PendingGrading` as “Đang chờ chấm điểm”, before the unpublished-result branch. This recovery state was checked in Razor source; it was not induced by damaging demo result data and has no screenshot evidence.

## Registration and usage guide extension

Actual Edge/Playwright browser verification after the registration build, with `Asia/Ho_Chi_Minh` timezone:

| Check | Outcome |
|---|---|
| Registration and Help pages at 1440 px and 360 px | Captured and visually inspected; readable labels and content, no overlapping elements |
| Mobile overflow | Register, Help and newly registered empty-session page: 360 viewport / 360 scroll width |
| Password confirmation mismatch | Vietnamese field error “Mật khẩu xác nhận không khớp.” visible; password fields cleared on response |
| Successful registration | Unique demo student created; redirected to Login with explicit success/assignment guidance |
| Login with new account | Successful; no assigned sessions shown, with clear empty-state guidance |
| Help navigation | Present when anonymous and authenticated |
| JavaScript page errors | Zero |

Evidence: `registration-results.json`, `register-desktop.png`, `register-mobile.png`, `register-mismatch-mobile.png`, `registered-empty-mobile.png`, `help-desktop.png`, `help-mobile.png`. Captures do not contain a visible password. Registration script creates a new disposable student each run; use demo data only.

## Staff registration extension

After the four-account-type update, actual browser checks verified:

- All four Vietnamese account-type options are present. Student code is visible/enabled/required only for Student; it is hidden/disabled/not required for Lecturer, ExamOfficer and Administrator.
- Lecturer and Administrator registrations without a student code succeeded and returned the explicit pending-approval notice directing approved staff to Desktop.
- Staff form captured at 360 px and 1440 px; mobile width/scroll width were both 360. Visually inspected staff form and pending notice: readable, no overlap.
- Zero JavaScript page errors. No staff account was approved from the Web test.
- Test fixtures: `staff_lecturer_mvmareao` (Lecturer) handed to Desktop QA for approval verification; `staff_administrator_mvmarfe2` (Administrator) left pending.

Evidence: `staff-registration-results.json`, `register-staff-mobile.png`, `register-staff-desktop.png`, `register-lecturer-pending-mobile.png`, `register-administrator-pending-mobile.png`.
