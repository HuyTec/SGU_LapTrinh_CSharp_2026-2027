# Desktop redesign verification

Offscreen WinForms DrawToBitmap captures on Windows. Actual forms and authorized API reads are used; no Save, Delete, Action, Import, seed or business-data updates are called. Authentication creates/revokes login sessions. These captures do not establish native interactive Windows usability. The 150% cases explicitly scale control bounds/fonts at the current device DPI; they are simulations, not native per-monitor DPI tests.

- PASS: Captured login-1366x768 (1366×768, device DPI 120).
- PASS: Captured login-1366x768-sim150 (1366×768, device DPI 120, simulated control/font scaling 150%).
- PASS: Captured login-1920x1080 (1878×949, device DPI 120).
- PASS: Captured login-1920x1080-sim150 (1878×949, device DPI 120, simulated control/font scaling 150%).
- PASS: Captured login-validation (1040×680, device DPI 120).
- PASS: Login local validation produced visible feedback without submitting credentials.
- PASS: Captured dashboard-admin-1366x768 (1366×768, device DPI 120).
- PASS: Captured dashboard-admin-compact (1366×768, device DPI 120).
- PASS: Captured resource-admin-users (1366×768, device DPI 120).
- PASS: Captured editor-admin-users (1100×760, device DPI 120).
- PASS: Constructed actual users editor from authorized metadata/detail; no save performed.
- PASS: Captured resource-admin-assignments (1366×768, device DPI 120).
- PASS: Captured editor-admin-assignments (1100×760, device DPI 120).
- PASS: Constructed actual assignments editor from authorized metadata/detail; no save performed.
- PASS: Captured dashboard-admin-1920x1080 (1920×1080, device DPI 120).
- PASS: Captured dashboard-admin-1366x768-sim150 (1366×768, device DPI 120, simulated control/font scaling 150%).
- PASS: Captured dashboard-admin-1920x1080-sim150 (1920×1080, device DPI 120, simulated control/font scaling 150%).
- PASS: Actual Administrator dashboard and authorized resource screens loaded.
- PASS: Captured dashboard-lecturer-1366x768 (1366×768, device DPI 120).
- PASS: Captured resource-lecturer-questions (1366×768, device DPI 120).
- PASS: Captured editor-lecturer-questions (1100×760, device DPI 120).
- PASS: Captured editor-lecturer-questions-bottom (1100×760, device DPI 120).
- PASS: Constructed actual questions editor from authorized metadata/detail; no save performed.
- PASS: Captured resource-lecturer-exams (1366×768, device DPI 120).
- PASS: Captured editor-lecturer-exams (1100×760, device DPI 120).
- PASS: Captured editor-lecturer-exams-bottom (1100×760, device DPI 120).
- PASS: Constructed actual exams editor from authorized metadata/detail; no save performed.
- PASS: Captured resource-lecturer-monitor (1366×768, device DPI 120).
- PASS: Captured dashboard-lecturer-1920x1080 (1920×1080, device DPI 120).
- PASS: Captured dashboard-lecturer-1366x768-sim150 (1366×768, device DPI 120, simulated control/font scaling 150%).
- PASS: Captured dashboard-lecturer-1920x1080-sim150 (1920×1080, device DPI 120, simulated control/font scaling 150%).
- PASS: Actual Lecturer dashboard and authorized resource screens loaded.
- PASS: Captured dashboard-officer-1366x768 (1366×768, device DPI 120).
- PASS: Captured resource-officer-sessions (1366×768, device DPI 120).
- PASS: Captured editor-officer-sessions (1100×760, device DPI 120).
- PASS: Captured editor-officer-sessions-step2 (1100×760, device DPI 120).
- PASS: Captured editor-officer-sessions-step3 (1100×760, device DPI 120).
- PASS: Constructed actual sessions editor from authorized metadata/detail; no save performed.
- PASS: Captured resource-officer-candidates (1366×768, device DPI 120).
- PASS: Captured editor-officer-candidates (1100×760, device DPI 120).
- PASS: Constructed actual candidates editor from authorized metadata/detail; no save performed.
- PASS: Captured resource-officer-monitor (1366×768, device DPI 120).
- PASS: Captured dashboard-officer-1920x1080 (1920×1080, device DPI 120).
- PASS: Captured dashboard-officer-1366x768-sim150 (1366×768, device DPI 120, simulated control/font scaling 150%).
- PASS: Captured dashboard-officer-1920x1080-sim150 (1920×1080, device DPI 120, simulated control/font scaling 150%).
- PASS: Actual ExamOfficer dashboard and authorized resource screens loaded.

## Targeted follow-up

- PASS: Actual UiButton normal/hover/pressed/focused/disabled renders captured for four kinds; all four outer corner pixels equal the parent background in all 20 cases, even with BackColor=Primary and AcceptButton enabled.
- PASS: Button keyboard focus obtained on the UI thread; its indicator is a rounded dotted path, with no native square focus rectangle.

## Targeted follow-up

- PASS: Captured resource-lecturer-questions (1366×768, device DPI 120).
- PASS: Targeted lecturer question preview re-rendered from the real authorized question bank without saving.
