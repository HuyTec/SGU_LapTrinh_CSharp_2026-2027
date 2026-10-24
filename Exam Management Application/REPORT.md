# Báo cáo triển khai và kiểm chứng — 24/10/2026

Báo cáo tổng hợp demo ngày 24/10/2026, vị trí mã nguồn, vận hành và cải thiện: [xem báo cáo](docs/BAO_CAO_DEMO_2026-10-24.md).

## Bổ sung hiện hành: đăng ký đủ vai trò và duyệt nhân sự

Đã mở rộng trang `/Account/Register` cho Thí sinh, Giảng viên, Cán bộ quản lý thi và Quản trị viên. Tất cả nhập tên đăng nhập, họ tên, email, mật khẩu và xác nhận; chỉ Thí sinh cần mã thí sinh. Thí sinh hoạt động ngay nhưng cần phân công đợt thi. Nhân sự có IsActive=false, RequestedRole và chưa có vai trò thực thi; quản trị viên hiện tại duyệt ở Desktop → Tài khoản / thí sinh → Duyệt đăng ký, sau đó nhân sự đăng nhập Desktop.

Migration `20261024110741_StaffRegistrationApproval` đã áp dụng, SQL idempotent đã cập nhật; EF kiểm tra không còn model pending. Web/backend build 0 lỗi/0 cảnh báo. Desktop build vào output riêng vì ứng dụng người dùng đang mở giữ DLL; không đóng cửa sổ người dùng. Người dùng cần đóng và chạy lại Start-Desktop.ps1 để cập nhật bản đang dùng.

Bằng chứng mới: 34/34 kiểm tra đăng ký/duyệt HTTP (`artifacts/integration/registration-results.json`), 26/26 xUnit, không skipped (`artifacts/tests/staff-registration.trx`), và 37/37 HTTP nghiệp vụ hồi quy (`artifacts/integration/http-results.json`). Đã kiểm tra nhân sự chưa có quyền trước duyệt, sai mật khẩu vẫn báo lỗi chung, chỉ Administrator được duyệt, unlock không vượt bước duyệt, cấp đúng vai trò và từ chối duyệt lặp. Đăng ký Student và chống trường Role/Roles giả mạo vẫn đạt.

Trình duyệt thật đã thử đăng ký Giảng viên và Quản trị viên không cần mã thí sinh, nhận thông báo chờ duyệt; 4 lựa chọn ẩn/hiện trường đúng, 360/1440 px không tràn ngang và 0 lỗi JavaScript (`artifacts/web/staff-registration-results.json`, `register-staff-mobile.png`). Hướng dẫn Web và Desktop đã đồng bộ luồng mới; bằng chứng render Desktop ghi trong `artifacts/desktop/qa.md`, vẫn phân biệt kiểm tra offscreen/API với thao tác cửa sổ Windows thực tế. Bảng FR dưới đây giữ nguyên baseline đã chốt; đăng ký nhân sự là phần bổ sung theo yêu cầu tiếp theo.

## Tóm tắt điều hành

### Sửa lỗi danh sách chủ đề cũ trên Desktop

Tài khoản `lecturer` đã có quyền và API trả chủ đề `Thiên văn` thuộc Lập trình C#. Lỗi nằm ở MainForm: nút điều hướng giữ catalog từ lúc đăng nhập, còn Làm mới không dựng lại bộ lọc. Đã tải lại catalog khi chuyển màn hình, dựng lại lookup khi Làm mới và giữ lựa chọn bộ lọc còn hợp lệ. Build Desktop riêng 0 lỗi/cảnh báo; kiểm tra Form thật qua API xác nhận chủ đề tạo sau đăng nhập xuất hiện khi chuyển màn, chủ đề tạo tiếp xuất hiện sau Làm mới, giữ bộ lọc khi làm mới và reset khi đổi màn. Kiểm tra quyền Lecturer2 vẫn không thấy chủ đề ngoài môn phân công. Hai chủ đề kiểm thử đã xóa, giữ nguyên dữ liệu người dùng. Bằng chứng: `artifacts/desktop/qa.md`, mục Fresh topic lookup regression; `07-topic-lookup-refreshed.png`. Đây là kiểm tra offscreen/API, không tự đóng hoặc điều khiển cửa sổ người dùng đang mở.

Kiểm tra Desktop bổ sung cho duyệt nhân sự: Form thật hiển thị cột Vai trò đăng ký và nút Duyệt đăng ký tại 1000×700 (`artifacts/desktop/06-pending-staff.png`). Desktop API client đã duyệt fixture Giảng viên do trình duyệt tạo; đăng nhập Giảng viên và tải catalog thành công. Chưa tự động thao tác hộp thoại xác nhận native; không thay đổi cửa sổ Desktop hoặc tài khoản thật đang được người dùng sử dụng.

- Đã tích hợp WinForms quản trị + Web MVC/Razor/Bootstrap + SQL Server dùng chung.
- 54/54 FR bắt buộc có triển khai và bằng chứng kiểm tra theo nhóm; 0 FR bắt buộc còn thiếu ở luồng cơ bản.
- 26/26 xUnit đạt trên SQL Server thật, không bỏ qua; 37 kiểm tra HTTP đạt; build 6 project không lỗi/cảnh báo.
- MR đã duyệt: xuất kết quả CSV/XLSX đạt; 5 FR mở rộng khác được hoãn theo quyết định.
- Đã kiểm tra Web thật ở 360/1440 px; Desktop dựng Form thật, gọi API và render ngoài màn hình.
- Đã đo 50 thí sinh đồng thời trên loopback; đã sao lưu và phục hồi sang DB riêng.
- Rủi ro còn lại lớn nhất: chưa thao tác Desktop thủ công trên màn hình, chưa chạy tải qua LAN/Internet và chưa diễn tập nhóm cuối.

“Đạt” trong bảng là có triển khai, review bất biến và bằng chứng nhóm tương ứng; không có nghĩa mọi nhánh AF/EF đã được tự động hóa hoặc hệ thống đã được đánh giá production. Bằng chứng có thời điểm riêng, không suy ra kiểm thử giao diện từ build. Kiểm tra cuối về RowVersion không thay đổi giao diện Web đã chụp trước đó.

## Bằng chứng dùng trong bảng

| Mã | Bằng chứng thực tế |
|---|---|
| T1 | `artifacts/tests/final-tests.trx`: 26 đạt, 0 lỗi, 0 skipped; `Exam.Tests/SqlExamTests.cs:89` bắt đầu các ca SQL đồng thời/quyền/snapshot/biên giờ/FK |
| T2 | `scripts/smoke_http.py:1`, `artifacts/integration/http-results.json`: 37 checks, tạo dữ liệu riêng; Desktop API → SQL → Web, chấm/công bố, worker, phân quyền, CSRF, import/export, thu hồi phiên, RowVersion, hoãn/hủy |
| W | `artifacts/web/qa.md`, `qa-results.json`, `resilience-results.json`, `published-results.json` và PNG: trình duyệt Edge/Playwright thật, tải lại/queue/retry/bàn phím/mobile/kết quả |
| D | `artifacts/desktop/qa.md` và PNG: Form WinForms thật, 3 vai trò, API CRUD, câu hỏi/điểm từng câu, token phiên bản ẩn; render offscreen, chưa phải thao tác OS thủ công |
| P | `artifacts/integration/performance.json`, `environment.json`: tải HTTP có barrier 50 client |
| B | `artifacts/integration/backup-restore.txt`: backup COPY_ONLY, checksum, restore riêng và đối chiếu 124 người dùng |

## FR bắt buộc

Tác nhân trong bảng là người triển khai: A1 backend, A2 WinForms, A3 Razor/Bootstrap; điều phối tạo kiểm thử, tích hợp và tài liệu. Đây không phải bảng gán đóng góp học tập cho các thành viên nhóm.

| FR | Mô tả ngắn | UC | Ứng dụng | Tác nhân | Trạng thái | Bằng chứng (file:dòng/test) |
|---|---|---|---|---|---|---|
| FR-A01 | cho phép người dùng đăng nhập bằng tên đăng nhập và mật khẩu | UC-01 | Web + Desktop | A1/A2/A3 | Đạt luồng cơ bản | Exam.Services/AuthService.cs:10; T2, W, D |
| FR-A02 | chỉ chấp nhận đăng nhập trên ứng dụng tương ứng với vai trò (Thí sinh: Web; các vai trò còn lại: Desktop) | UC-01 | Web + Desktop | A1/A2/A3 | Đạt luồng cơ bản | Exam.Services/AuthService.cs:10; T2, W, D |
| FR-A03 | từ chối đăng nhập và hiển thị thông báo khi thông tin xác thực sai hoặc tài khoản bị khóa | UC-01 | Web + Desktop | A1/A2/A3 | Đạt luồng cơ bản | Exam.Services/AuthService.cs:10; T2, W, D |
| FR-A04 | phải kiểm tra vai trò và phạm vi dữ liệu của người dùng tại máy chủ cho mọi yêu cầu nghiệp vụ | Mọi UC | Web + Desktop | A1/A2/A3 | Đạt luồng cơ bản | Exam.Services/AuthService.cs:10; T2, W, D |
| FR-A05 | cho phép người dùng đăng xuất và hủy phiên đăng nhập | UC-02 | Web + Desktop | A1/A2/A3 | Đạt luồng cơ bản | Exam.Services/AuthService.cs:10; T2, W, D |
| FR-B01 | hiển thị cho thí sinh danh sách đợt thi được phân công, kèm lịch, thời lượng và trạng thái | UC-05 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:12; T1, T2, W |
| FR-B02 | cho phép thí sinh tìm và lọc danh sách đợt thi theo tên hoặc trạng thái | UC-05 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:12; T1, T2, W |
| FR-B03 | hiển thị thông tin chi tiết và hướng dẫn làm bài của đợt thi trước khi thí sinh bắt đầu | UC-06 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:19; T1, T2, W |
| FR-B04 | chỉ cho thí sinh bắt đầu thi khi thí sinh có trong danh sách phân công, đợt thi đang mở và chưa dùng lượt thi | UC-06 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:31; T1, T2, W |
| FR-B05 | từ chối bắt đầu và nêu lý do khi thí sinh chưa đến giờ, không được phân công, đợt thi đã đóng hoặc đã dùng lượt thi | UC-06 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:31; T1, T2, W |
| FR-B06 | tạo đúng một lượt thi cho mỗi thí sinh trong mỗi đợt thi; tải lại trang hoặc đăng nhập lại không tạo lượt mới | UC-06 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:31; T1, T2, W |
| FR-B07 | sao chép bất biến câu hỏi, phương án, điểm và thứ tự vào lượt thi khi tạo lượt | UC-06 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:31; T1, T2, W |
| FR-B08 | trộn thứ tự câu hỏi và phương án theo cấu hình đợt thi và giữ ổn định trong cùng một lượt thi | UC-06 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:31; T1, T2, W |
| FR-B09 | xác định hạn kết thúc theo thời điểm sớm hơn giữa “bắt đầu \+ thời lượng” và “giờ đóng đợt thi” | UC-06 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:31; T1, T2, W |
| FR-B10 | hiển thị thời gian còn lại cho thí sinh dựa trên giờ máy chủ | UC-06 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:40; T1, T2, W |
| FR-B11 | cho phép thí sinh chọn một phương án cho mỗi câu và đổi lựa chọn khi lượt thi còn hiệu lực | UC-06 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:53; T1, T2, W |
| FR-B12 | lưu từng câu trả lời ngay khi thí sinh chọn và báo trạng thái lưu thành công hoặc thất bại | UC-06 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:53; T1, T2, W |
| FR-B13 | từ chối lưu câu trả lời gửi đến sau hạn kết thúc hoặc sau khi bài đã nộp | UC-06, 07 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:53; T1, T2, W |
| FR-B14 | cho phép thí sinh chuyển giữa các câu và đánh dấu câu cần xem lại | UC-06 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:40; T1, T2, W |
| FR-B15 | khôi phục lượt thi đang chạy khi tải lại trang hoặc đăng nhập lại, giữ nguyên hạn kết thúc và các câu đã lưu thành công | UC-06 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:40; T1, T2, W |
| FR-B16 | cho phép thí sinh chủ động nộp bài sau khi xác nhận | UC-07 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:54; T1, T2, W |
| FR-B17 | tự kết thúc lượt thi khi hết hạn, dựa trên các câu trả lời đã ghi nhận, kể cả khi thí sinh đã đóng trình duyệt | UC-07 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:54; T1, T2, W |
| FR-B18 | khóa việc sửa bài sau khi nộp hoặc hết giờ | UC-07 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:54; T1, T2, W |
| FR-B19 | không tạo bài làm hoặc kết quả mới khi nhận lại yêu cầu nộp của lượt đã nộp | UC-07 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:54; T1, T2, W |
| FR-B20 | tự động chấm lượt thi đã kết thúc bằng đáp án và điểm của nội dung đã giao trong lượt đó | UC-07 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:54; T1, T2, W |
| FR-B21 | tính điểm theo thang 10 là tổng điểm các câu đúng; câu sai hoặc bỏ trống được 0 điểm, không trừ điểm | UC-07 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:54; T1, T2, W |
| FR-B22 | lưu một kết quả duy nhất gắn với mỗi lượt thi | UC-07 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:54; T1, T2, W |
| FR-B23 | chỉ hiển thị điểm cho thí sinh sau khi kết quả được công bố | UC-08 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:68; T1, T2, W |
| FR-B24 | chỉ hiển thị bài làm và đáp án đúng cho thí sinh trong phạm vi chính sách công bố cho phép | UC-08 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:68; T1, T2, W |
| FR-B25 | hiển thị lịch sử các lượt thi của chính thí sinh (đợt thi, thời điểm, trạng thái, điểm nếu đã công bố) | UC-09 | Web + worker | A1/A3 | Đạt luồng cơ bản | Exam.Services/ExamService.cs:76; T1, T2, W |
| FR-C01 | cho phép giảng viên thêm câu hỏi (nội dung, chủ đề, độ khó) cùng các phương án trả lời và đánh dấu đáp án đúng | UC-10.1 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C02 | chỉ lưu câu hỏi có ít nhất hai phương án và đúng một phương án đúng | UC-10.1, 10.2 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C03 | chỉ cho giảng viên quản lý chủ đề, câu hỏi và đề thi thuộc môn học được phân công | UC-10, 11, 22 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C04 | cho phép giảng viên sửa nội dung, phương án và đáp án của câu hỏi | UC-10.2 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C05 | bảo toàn nội dung, đáp án và kết quả của các lượt thi đã ghi nhận khi câu hỏi được sửa hoặc ngừng sử dụng | UC-10.2, 10.3 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C06 | cho phép ngừng sử dụng câu hỏi: câu hỏi không được chọn vào đề mới nhưng dữ liệu cũ được giữ | UC-10.3 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C07 | chỉ cho xóa hẳn câu hỏi chưa từng nằm trong đề hoặc lượt thi nào | UC-10.3 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C08 | cho phép tìm kiếm, lọc câu hỏi theo môn, chủ đề, độ khó và phân trang phía máy chủ | UC-10 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:84; T2, D |
| FR-C10 | cho phép giảng viên thêm, đổi tên chủ đề thuộc môn được phân công (tên không trùng trong cùng môn) và chỉ xóa chủ đề chưa có câu hỏi | UC-22 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C11 | cho phép giảng viên tạo đề thi bằng cách khai báo tên đề và môn học | UC-11.1 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C12 | cho phép chọn vào đề các câu hỏi hợp lệ (đang sử dụng, cùng môn học của đề) từ ngân hàng | UC-11.1 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C14 | cho phép xác định điểm từng câu và yêu cầu tổng điểm đề bằng 10 | UC-11.3 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C15 | kiểm tra số câu, nội dung, đáp án và cấu trúc của đề trước khi cho xác nhận sẵn sàng và thông báo từng lỗi | UC-11.3 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C16 | cho phép lưu đề ở trạng thái nháp hoặc xác nhận sẵn sàng sử dụng | UC-11.3 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-C17 | không cho sửa trực tiếp đề đã gắn với đợt thi đã mở; thay đổi để dùng về sau phải tạo đề hoặc phiên bản mới | UC-11 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-D01 | Theo dõi trạng thái tham gia trong đúng môn/đợt được giao | UC-12 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-D03 | Chỉ công bố khi đợt đóng và mọi lượt phát sinh đã được chấm; lưu thời điểm công bố | UC-21 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-D04 | Tạo đợt từ đề sẵn sàng, cấu hình lịch/thời lượng/trộn/chính sách; kích hoạt khóa cấu hình | UC-18 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-D05 | Phân công thí sinh hoạt động, không trùng hoặc giao lịch; chỉ sửa khi nháp chưa từng mở | UC-19 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-D06 | Hoãn/hủy trước lần mở đầu và trước khi phát sinh lượt; hoãn về nháp để kích hoạt lại | UC-20 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-D07 | Thêm/sửa/tìm môn; chỉ xóa khi chưa tham chiếu, nếu đã dùng thì ngừng sử dụng | UC-17 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-E01 | Tạo/sửa/gán vai trò, khóa/mở khóa và đặt lại mật khẩu; thu hồi phiên khi quyền thay đổi | UC-14 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-E02 | Tạo hồ sơ thí sinh, mã duy nhất, không ghi đè dữ liệu cũ; nhập tệp là nhánh cần báo cáo mức triển khai thực tế | UC-15 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |
| FR-E03 | Thêm/gỡ phân công giảng viên hoạt động theo môn, không tạo cặp lặp và giữ tác giả dữ liệu cũ | UC-16 | Desktop + API | A1/A2 | Đạt luồng cơ bản | Exam.Services/AdminService.cs:167; T2, D |

## FR mở rộng và phần đã duyệt

| FR | Nội dung | Trạng thái / bằng chứng |
|---|---|---|
| FR-D02 | Tra cứu và xuất kết quả | Đạt phần đã duyệt: CSV UTF-8 BOM và XLSX, đúng phạm vi, chưa thi khác điểm 0; T2. Chưa có dashboard thống kê tổng hợp/biểu đồ; không tính phần này là đã hoàn thành. |
| FR-A06 | Hồ sơ cá nhân tự cập nhật | Hoãn; quản trị sửa tài khoản vẫn có ở E01 |
| FR-A07 | Người dùng tự đổi mật khẩu | Hoãn; quản trị đặt lại mật khẩu đã có và đã thử HTTP |
| FR-B26 | Giám sát mất kết nối | Hoãn; Web báo lỗi lưu và cho thử lại không thay thế giám sát mất kết nối phía cán bộ |
| FR-C09 | Nhập/xuất ngân hàng câu hỏi Excel | Hoãn; khác xuất kết quả CSV/XLSX và nhập thí sinh CSV |
| FR-C13 | Sinh đề theo ma trận | Hoãn; chọn câu và điểm thủ công đã có |

## Build, dữ liệu và kiểm thử thực tế

Môi trường: Windows, SDK 10.0.401; .NET/EF Core 10.0.13 cho các gói ứng dụng, SQL Server Express 17.0.1000.7; i5-13450HX, 16 luồng, RAM 15.71 GiB. Xem `artifacts/integration/environment.json`. Phiên bản runtime cài trên máy là 10.0.12; không suy phiên bản runtime từ gói EF.

Các lệnh đã chạy (cấu hình nhạy cảm lấy từ user secrets hoặc biến môi trường, không ghi tại đây):

```powershell
dotnet build 'Exam Management Application.slnx' --no-restore -m:1
dotnet ef database update --project Exam.Data --startup-project Exam.Web --no-build
dotnet ef migrations script --idempotent --project Exam.Data --startup-project Exam.Web --no-build --output docs/schema.sql
dotnet ef migrations has-pending-model-changes --project Exam.Data --startup-project Exam.Web --no-build
dotnet test Exam.Tests/Exam.Tests.csproj --no-build --no-restore -m:1 --logger 'trx;LogFileName=final-tests.trx' --results-directory artifacts/tests
python scripts/smoke_http.py
python scripts/performance_http.py
.\scripts\Backup-Restore-Check.ps1
```

Lệnh Python dùng Python 3 có sẵn trong runtime Codex ở phiên này; người dùng có thể dùng Python 3 thông thường. Ca SQL chạy với `EXAM_TEST_CONNECTION` trỏ SQL Express, mỗi ca tạo/xóa DB `ExamAppTests_<GUID>` riêng. `Test-Local.ps1` thiết lập biến này. Không thay bằng SQLite/InMemory và không tính ca bỏ qua là đạt.

- Build toàn solution 6 project: **0 warning, 0 error**, log `artifacts/build.txt`.
- Migration `InitialSchema`, `PublicationTimestamp`, `QuestionConcurrency` đã áp dụng trên ExamAppDb; kiểm tra không còn model pending.
- 26 xUnit: 10 ca quy tắc/biên, 11 ca SQL, 5 ca parser CSV; TRX ghi 0 lỗi, 0 skipped.
- 37 checks HTTP: bao gồm nộp lặp; hai người không xem/sửa bài nhau; worker tự chấm khi không còn request thí sinh; không lộ đáp án trước công bố; CSV preview/atomic; nhiều vai trò; khóa/đặt mật khẩu/đăng xuất; xung đột sửa câu hỏi; đề về nháp và bảo vệ đề đã kích hoạt; hoãn/hủy; gỡ quyền môn.
- XLSX kiểm tra cấu trúc ZIP/OpenXML và dữ liệu; CSV đối chiếu đúng thí sinh, điểm 10; chưa mở Excel bằng giao diện để kiểm tra in ấn.
- NuGet audit trên cả 6 project không báo gói có lỗ hổng đã biết tại thời điểm kiểm tra; đây không phải chứng nhận bảo mật.
- Backup SQL cuối có checksum và restore riêng thành công, source/restored cùng **124 users**; chỉ xóa DB restore do script vừa tạo.

### Hiệu năng NFR-04

Đo trên **HTTP loopback cùng một máy Windows**, 50 thí sinh khác nhau, barrier cho 50 request Save và 50 request danh sách. p95 Save **52.35 ms** (max 644.32 ms); p95 danh sách **27.16 ms** (max 28.81 ms). Đạt mốc đề xuất 2 giây/3 giây trong môi trường đo này. Không suy ra sức chịu tải LAN/Internet hoặc tải kéo dài. Tải được đo trước bản vá token câu hỏi quản trị cuối; luồng Save bài thi giữ nguyên.

### Giao diện và khả năng phục hồi

Web đã kiểm tra ở 360 và 1440 px, không tràn ngang ở các trang thử; thao tác bàn phím, focus/xác nhận nộp, Escape, lưu lỗi/thử lại, đổi lựa chọn nhanh rồi nộp, khôi phục sau reload và giờ Việt Nam. Kết quả công bố 10 điểm cùng 3 câu được kiểm tra bằng trình duyệt thật. Chưa đánh giá screen reader đầy đủ. Trạng thái hiếm `PendingGrading` có nhánh Razor rõ ràng nhưng chỉ kiểm tra nguồn, chưa gây lỗi chấm có chủ đích để chụp trạng thái này.

Desktop đã dựng và render Form thật bằng DrawToBitmap, dùng API client thật để CRUD môn, lưu câu hỏi/đáp án đúng và đề 3.33 + 3.33 + 3.34; Form sửa gửi token phiên bản đúng, server tạo token mới. Harness có xử lý render footer riêng cùng bounds vì DrawToBitmap có giới hạn native controls; xem ghi chú QA. Chưa kiểm thử người dùng kéo/resize/click toàn bộ quy trình trên cửa sổ Windows hiển thị.

## Phát hiện còn mở và đề xuất

| Mức | Còn mở | Đề xuất |
|---|---|---|
| P1 trước nộp | Desktop mới được kiểm tra offscreen và API thật | Mỗi vai trò chạy `Start-Desktop.ps1`, thao tác một vòng tạo → thi Web → công bố → xuất trên màn hình Windows, ghi ảnh thực tế |
| P1 trước nộp | Chưa diễn tập trên máy/mạng phòng demo | Dựng từ README, dùng DB riêng, kiểm tra SQL quyền/port và kết nối hai client; dùng HTTPS khi đưa ra ngoài loopback |
| P2 | DOCX phân tích cũ và báo cáo môn học chưa được biên soạn lại toàn bộ | Xem DOCX là lịch sử; dùng Markdown/schema hiện tại làm nguồn, bổ sung bìa/mục lục/ảnh và phân công thật của nhóm |
| P2 | Chưa thống kê tổng hợp, ma trận đề, Excel câu hỏi và các MR hoãn | Chỉ triển khai khi còn thời gian sau diễn tập bắt buộc; không ghi là đã có |
| P2 | Chưa đo tải dài hạn hoặc tình huống host chết giữa giao dịch/khôi phục | Bổ sung thử nghiệm riêng trên bản sao dữ liệu; không thay đổi DB đang demo |
| P3 | Đoạn service cũ còn nhiều câu lệnh ngắn trên một dòng | Tách định dạng/phương thức nhỏ ở lượt bảo trì sau khi chốt demo; giữ nguyên kiểm thử và hợp đồng |

Không còn lỗi build/test đã biết chặn luồng chính trong phạm vi kiểm tra. Không coi điều này là kết luận không còn lỗi.

## Quyết định và hợp đồng đã thực hiện

Nguồn duyệt: `DECISIONS.md` với lựa chọn 1a–5a và phạm vi bắt buộc + xUnit + CSV/XLSX. WinForms được chọn cho sản phẩm mới; `App1` WinUI được giữ nguyên làm tham khảo. Web/API dùng một host; Form/View/JS không kết nối SQL hoặc chấm điểm. Nghiệp vụ nằm ở Exam.Services; Exam.Core chứa DTO, Exam.Data chứa entity/mapping.

- Identity dùng đúng `Users`, `Roles`, `UserRoles`, int UserID/RoleID, không có bảng người dùng nghiệp vụ song song. Bảng kỹ thuật Identity và AuthSessions quản lý xác thực/thu hồi.
- Schema có ManagedBy, IsFlagged, shuffle, review policy, PublishedAt, trạng thái và snapshot; PK/FK/UQ/CHECK cùng giao dịch bảo vệ dữ liệu. RowVersion bổ sung chống form cũ ghi đè câu hỏi.
- `questions` GET trả `version` base64; PUT cần token gốc, thiếu/sai/cũ trả 409. Metadata `hidden` không hiện thành ô nhập hoặc cột bảng.
- API JWT cho nhân sự, Cookie/antiforgery cho thí sinh. Mỗi phiên nhân sự chọn một vai trò hoạt động từ các vai trò Identity của mình.
- UC-17 thuộc Cán bộ quản lý thi; sơ đồ XML và Markdown đã sửa actor tương ứng. Chưa tuyên bố visual QA bố cục toàn bộ sơ đồ hoặc cập nhật DOCX lịch sử.
- Bổ sung FR-D/E để truy vết các UC bắt buộc đã có, đưa search B02 và xóa câu chưa dùng C07 vào baseline; tổng bảng 60 FR = 54 bắt buộc + 1 MR duyệt phần xuất + 5 MR hoãn.
- Chi tiết route/DTO: `CONTRACT.md`; schema vật lý/3NF và ngoại lệ snapshot lịch sử: `docs/SCHEMA.md`; SQL tái chạy: `docs/schema.sql`.

## Việc tiếp theo theo ưu tiên

1. Chạy diễn tập Desktop hiển thị với 3 tài khoản nhân sự và một thí sinh; dùng `New-Demo.ps1 -Minutes 5` để tạo lịch mới.
2. Dựng trên máy demo từ `Initialize-Local.ps1`; kiểm tra Web/Desktop cùng SQL Server; sao lưu và giữ cấu hình cục bộ.
3. Hoàn thiện báo cáo môn học theo cấu trúc yêu cầu, ảnh từ demo thật và bảng đóng góp do nhóm xác nhận; không lấy phân công A1/A2/A3 làm đóng góp sinh viên.
4. Sau khi ổn định mới cân nhắc MR đã hoãn. Không để MR thay thế tính chạy được và giải thích được mã nguồn.
