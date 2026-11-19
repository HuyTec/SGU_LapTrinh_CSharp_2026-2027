> Phạm vi cập nhật theo [DECISIONS.md](../DECISIONS.md), rà soát ngày 19/11/2026: Desktop WinForms cho Quản trị viên; Web cho Thí sinh và Giảng viên; .NET/EF Core 10; SQL Server trong Docker; Identity Users/Roles/UserRoles. UC-17 thuộc Quản trị viên. Các bảng dưới đây là yêu cầu nghiệm thu, không phải xác nhận tính năng đã hoàn thành.

# YÊU CẦU DỰ ÁN — ĐỀ TÀI 18: HỆ THỐNG THI TRẮC NGHIỆM TRỰC TUYẾN

> Tài liệu này tổng hợp **yêu cầu của đề môn** (nguồn: "Đề tài môn Ngôn ngữ lập trình C# – 2026", ban hành 20/09/2026) và **đối chiếu với đặc tả hiện có** (`HỆ_THỐNG_QUẢN_LÝ_THI_TRẮC_NGHIỆM.md`, mục 1.3.3, 2.x, 3.1–3.3).
> Quy ước: **[Đề]** = yêu cầu từ đề môn; **[Nhóm]** = quyết định/giải pháp của nhóm; **[Cần xác nhận]** = đề không nói rõ.

---

## 1. Thông tin chung

| Mục | Nội dung |
|---|---|
| Môn học | Ngôn ngữ lập trình C# |
| Giảng viên | TS. Đỗ Như Tài |
| Đề tài | Số 18 (Nhóm 3 – Giáo dục, đào tạo): Xây dựng hệ thống thi trắc nghiệm trực tuyến |
| Quy mô nhóm | 1 đề tài / 1 nhóm / 4 sinh viên |
| Ngày ban hành đề | 20/09/2026 |
| Hạn nộp, hình thức nộp, lịch báo cáo | **[Cần xác nhận]** — đề không nêu |

---

## 2. Phạm vi bắt buộc

**[Đề]** Hệ thống gồm **hai thành phần liên kết** cùng phục vụ nghiệp vụ và **dùng chung cơ sở dữ liệu**:

1. **Ứng dụng quản trị Desktop** chạy trên Windows bằng C#.
2. **Trang người dùng Web** chạy bằng C#.

**[Nhóm]** Áp dụng cho đề tài thi trắc nghiệm:

| Thành phần | Đối tượng dùng | Nhóm chức năng (theo 3.2.1) |
|---|---|---|
| Web | Thí sinh, Giảng viên | Tài khoản/hồ sơ; thi và xem kết quả; giảng viên quản lý học liệu, lớp trong phạm vi môn được giao và tổ chức đợt thi |
| Desktop | Quản trị viên | Tài khoản, thí sinh, phân công giảng viên, môn học, lớp và thành viên |
| CSDL | SQL Server trong Docker | Một CSDL nghiệp vụ dùng chung, truy cập qua máy chủ ứng dụng |

> Ba vai trò hiện hành là Student/Lecturer/Administrator. UC-17 giao Quản trị viên trên Desktop. Giảng viên được tạo lớp trên Web trong môn đang hoạt động được phân công; quyền này bổ sung cho luồng quản trị lớp trên Desktop.

---

## 3. Yêu cầu công nghệ

### 3.1. Bắt buộc và khuyến khích

| Hạng mục | Bắt buộc [Đề] | Khuyến khích [Đề] | Lựa chọn của nhóm [Nhóm] |
|---|---|---|---|
| Desktop | C# + **Windows Forms** | WinUI 3 hoặc công nghệ desktop .NET hiện đại (dành cho SV khá, giỏi) | Windows Forms |
| Web | C# | ASP.NET Core MVC / Razor Pages / Blazor; responsive | ASP.NET Core MVC + Razor + Bootstrap |
| CSDL | **Microsoft SQL Server** | — | SQL Server 2022 trong Docker |
| Truy cập dữ liệu | — | Entity Framework Core | EF Core + Migrations |
| Tích hợp | — | Web API | Web API (HTTPS + JSON) cho Desktop |
| Kiến trúc | — | Phân lớp, Dependency Injection, kỹ thuật .NET hiện đại | 3 lớp: giao diện – nghiệp vụ – dữ liệu; lõi dùng chung |
| Tài khoản | Đăng nhập, xác thực, phân quyền (Desktop) | — | ASP.NET Core Identity (cookie cho Web, token có thời hạn cho API) |
| Kiểm thử | — | Unit Test | xUnit cho quy tắc nghiệp vụ |

**[Đề]** Công nghệ mới là yếu tố khuyến khích, **không thay thế** yêu cầu chức năng bắt buộc; mọi công nghệ chọn phải giải thích được, triển khai ổn định và phù hợp phạm vi.

### 3.2. Nguyên tắc kiến trúc [Nhóm]

- **Backend dùng chung:** toàn bộ nghiệp vụ và truy cập dữ liệu nằm trong một lõi (`Exam.Core`, `Exam.Data`, `Exam.Services`), dùng cho cả Desktop và Web.
- Controller và Form **không chứa nghiệp vụ**; Desktop **không kết nối DB trực tiếp**, chỉ gọi API.
- Nền tảng đã chọn: .NET 10, EF Core 10, SQL Server 2022 trong Docker; Web/Desktop chạy bằng .NET trên Windows. Web phải kiểm tra kết nối SQL Docker khi khởi động, không tự chuyển sang SQL Express.

---

## 4. Yêu cầu tối thiểu theo đề và cách đáp ứng

### 4.1. Web người dùng [Đề]

| Yêu cầu tối thiểu | Đáp ứng trong đề tài thi trắc nghiệm [Nhóm] |
|---|---|
| Giao diện chính | Trang chủ thí sinh |
| Danh mục thông tin | Danh mục đợt thi (theo môn, trạng thái) |
| Tìm kiếm thông tin | Tìm kiếm đợt thi theo tên/môn/trạng thái (UC-05) |
| Menu thể hiện các chức năng | Menu: đợt thi, lịch sử thi, kết quả, hồ sơ |
| Chi tiết | Chi tiết đợt thi |
| Chức năng nghiệp vụ | Làm bài (UC-06), nộp và chấm tự động (UC-07), xem kết quả (UC-08), lịch sử (UC-09) |
| Responsive | Dùng được từ 360 px (NFR-05) |

### 4.2. Desktop quản trị [Đề]

| Yêu cầu tối thiểu | Đáp ứng [Nhóm] |
|---|---|
| Đăng nhập, xác thực, phân quyền trước khi dùng | UC-01; Desktop dành cho Administrator; hệ thống có ba vai trò Student/Lecturer/Administrator |
| Màn hình đăng nhập: tài khoản/mật khẩu, kiểm tra quyền | UC-01 |
| Thêm / xóa / sửa / xem / tìm kiếm | UC-14/15/16 (tài khoản, thí sinh, phân công), UC-17 (môn), UC-24 (lớp/thành viên) |
| Quản lý danh mục nghiệp vụ | Tài khoản, môn học, phân công giảng viên, lớp và thành viên; học liệu/đợt thi do giảng viên quản lý trên Web |

> ⚠️ **Điểm dễ mất điểm:** đề yêu cầu có chức năng **xóa**, nhưng đặc tả quy định không xóa tùy ý dữ liệu đã liên quan đến bài thi. Cần thể hiện rõ trong demo: xóa thật với dữ liệu chưa được tham chiếu, và "ngừng sử dụng" (xóa mềm) với dữ liệu đã dùng (UC-10.3).

---

## 5. Quy trình phân tích – thiết kế mà đề yêu cầu nắm được

**[Đề]** Sinh viên phải thực hiện đầy đủ:

1. Xác định mục tiêu, yêu cầu, giới hạn đề tài.
2. Khảo sát các hệ thống đã có (đã có: Moodle, Azota, ScoreUp).
3. Phân tích chức năng và **layout từng giao diện**.
4. Thiết kế các màn hình hợp lý (Web và Desktop).
5. Thiết kế lược đồ CSDL dùng chung, **đạt chuẩn 3NF**.
6. Triển khai CSDL trên SQL Server theo lược đồ.
7. Xây dựng Web: giao diện chính, danh mục, tìm kiếm, chi tiết, chức năng nghiệp vụ.
8. Xây dựng Desktop: đăng nhập, phân quyền, CRUD, quản lý danh mục.

> Khi nghiệm thu thiết kế giao diện, đối chiếu màn hình theo vai trò, UC phục vụ và trạng thái lỗi/dữ liệu rỗng theo AF/EF (xem mục 9). Đánh giá mức hoàn thành từ bản tài liệu và giao diện thực tế.

---

## 6. Rubric chấm điểm và cách đáp ứng

| # | Tiêu chí | Điểm | Nội dung chính [Đề] | Bằng chứng cần có [Nhóm] |
|---|---|---|---|---|
| 1 | Phân tích và thiết kế hệ thống | 1,0 | Mục tiêu, phạm vi, yêu cầu; khảo sát; phân tích chức năng; layout Web và Desktop | Chương 1–3 báo cáo, khảo sát 3 hệ thống, UC/FR, **mockup/layout** |
| 2 | Cơ sở dữ liệu | 1,0 | Đúng nghiệp vụ; PK/FK hợp lý; **3NF**; triển khai SQL Server; dùng chung | ER, bảng luận lý, phân tích 3NF, script SQL, thể hiện dùng chung |
| 3 | Ứng dụng quản trị Desktop | 1,5 | Đăng nhập, phân quyền; xem/thêm/sửa/xóa/tìm kiếm; quản lý nghiệp vụ; giao diện rõ; **xử lý lỗi hợp lý** | Demo CRUD + phân quyền, thông báo lỗi |
| 4 | Ứng dụng Web người dùng | 1,5 | Trang chủ, menu, danh mục, chi tiết, tìm kiếm; nghiệp vụ chính; kết nối CSDL; thân thiện, **responsive** | Demo luồng thi trên điện thoại và máy tính |
| 5 | Lập trình C# và kiến trúc | 1,0 | OOP; tổ chức mã; tách lớp giao diện–nghiệp vụ–dữ liệu; tái sử dụng; validation, exception; giải thích được mã | Cây solution, sơ đồ lớp/kiến trúc, xử lý ngoại lệ |
| 6 | Tích hợp và hoàn thiện | 1,0 | Web–Desktop–SQL Server thống nhất; dữ liệu đồng bộ; luồng nghiệp vụ đầy đủ, ổn định | Kịch bản demo xuyên suốt (mục 10) |
| 7 | Báo cáo | 1,0 | Đủ mục yêu cầu; có sơ đồ, CSDL, hình minh họa; trình bày tốt; tài liệu tham khảo | Mục 8 |
| 8 | Demo và vấn đáp | 1,0 | Demo Web, Desktop, liên kết dữ liệu; **từng SV giải thích được** phần việc, mã, kiến trúc | Phân công phần việc từng SV (mục 11) |
| 9 | Sáng tạo và mở rộng | 1,0 | Chức năng mở rộng hoặc công nghệ phù hợp (WinUI 3, Blazor, EF Core, Web API, DI, Unit Test, dashboard, xuất báo cáo…) | Mục 7 |
| | **Tổng** | **10,0** | | |

---

## 7. Phạm vi mở rộng (phục vụ tiêu chí 9)

Các mở rộng đã mô hình hóa trong đặc tả (ký hiệu MR), tách khỏi phần bắt buộc:

| Mở rộng | UC/FR | Ghi chú |
|---|---|---|
| Hồ sơ cá nhân, tự đổi mật khẩu | UC-03, UC-04 | |
| Nhập/xuất câu hỏi Excel | UC-10.4 | |
| Sinh đề theo ma trận | UC-11.2 | |
| Ghi nhận mất kết nối để giảng viên theo dõi | FR-B26 (UC-06, UC-12) | Hoãn theo DECISIONS.md |
| Tra cứu thống kê, xuất kết quả | UC-13 | Đáp ứng ví dụ "dashboard, xuất báo cáo" của đề |
| Unit Test | xUnit | Cho quy tắc nghiệp vụ (thời hạn, một lượt/đợt, chấm điểm) |
| WinUI 3 | — | Chỉ cân nhắc nếu nhóm đủ năng lực; **không bắt buộc** và không thay WinForms |

**Ngoài phạm vi (đã chốt trong 3.2.6):** thi offline, thi lại trong cùng đợt, tự luận hoặc nhiều đáp án đúng, phúc khảo/sửa điểm thủ công, giám sát AI/webcam, SSO MS365, khôi phục mật khẩu bằng email.

> Nguyên tắc ưu tiên: **hoàn thiện chức năng bắt buộc trước, mở rộng sau** (đề nhấn mạnh công nghệ mới không thay thế chức năng bắt buộc).

---

## 8. Yêu cầu báo cáo

### 8.1. Mục bắt buộc [Đề] và vị trí trong tài liệu hiện có

| Mục bắt buộc | Vị trí hiện có [Nhóm] | Việc còn lại |
|---|---|---|
| Trang bìa | — | Cần có |
| Mục lục | — | Cần có |
| Giới thiệu và mục tiêu đề tài | Chương 1 | Rà soát |
| Khảo sát hệ thống | Mục 2.1 (hệ thống tương tự), 2.2–2.7 (công nghệ) | Rà soát |
| Phân tích hệ thống (chức năng, layout…) | Chương 3 (3.1, 3.2) | **Bổ sung layout từng giao diện** |
| Thiết kế hệ thống (lược đồ CSDL…) | 3.3 (ý niệm, luận lý, vật lý) | Bổ sung 5 điểm ở mục 9; thiết kế màn hình |
| Kết quả đạt được | Chương kết quả | Sau khi có sản phẩm: ảnh chụp, kết quả test |
| Kết luận và hướng phát triển | Chương kết luận | Sau khi có sản phẩm |
| Tài liệu tham khảo | — | **Trích dẫn đầy đủ** |

### 8.2. Quy tắc trích dẫn [Đề]
Khi dùng kiến thức hoặc mã nguồn bên ngoài phải **giải thích và trích dẫn nguồn**.
**[Cần xác nhận]** Với mã do công cụ AI hỗ trợ sinh ra, nên hỏi giảng viên có yêu cầu khai báo hay không, và ghi nhận rõ trong báo cáo.

---

## 9. Yêu cầu chức năng chính (tóm tắt từ đặc tả)

> Chi tiết FR, user story, đặc tả UC xem mục 3.2 của đặc tả. Ở đây chỉ tóm tắt để làm tài liệu đối chiếu.

### 9.1. Danh sách UC theo nhóm

| Nhóm | UC | Tên | Tác nhân | Ứng dụng |
|---|---|---|---|---|
| A | 01, 02 | Đăng nhập, đăng xuất | Mọi vai trò | Desktop, Web |
| A (MR) | 03, 04 | Hồ sơ cá nhân, đổi mật khẩu | Mọi vai trò | Desktop, Web |
| B | 05 | Xem danh sách đợt thi | Thí sinh | Web |
| B | 06 | Làm bài thi trắc nghiệm | Thí sinh | Web |
| B | 07 | Nộp bài và tự động chấm điểm | Thí sinh | Web |
| B | 08 | Xem kết quả | Thí sinh | Web |
| B | 09 | Xem lịch sử bài thi | Thí sinh | Web |
| C | 10 (10.1–10.3; 10.4 MR) | Quản lý ngân hàng câu hỏi | Giảng viên | Web |
| C | 11 (11.1, 11.3; 11.2 MR) | Xây dựng đề thi | Giảng viên | Web |
| C | 22 | Quản lý chủ đề | Giảng viên | Web |
| D | 12 | Theo dõi tình trạng tham gia | Giảng viên | Web |
| D | 13 | Tra cứu thống kê, xuất kết quả | Giảng viên | Web |
| D | 18 | Tổ chức đợt thi | Giảng viên | Web |
| D | 19 | Gán lớp dự thi cùng môn | Giảng viên | Web |
| D | 20 | Hoãn/hủy đợt thi trước lần mở đầu | Giảng viên | Web |
| D | 21 | Công bố kết quả | Giảng viên | Web |
| E | 14 | Quản lý tài khoản và phân quyền | Quản trị viên | Desktop |
| E | 15 | Tạo/nhập danh sách thí sinh | Quản trị viên | Desktop |
| E | 16 | Phân công giảng viên theo môn | Quản trị viên | Desktop |
| E | 17 | Quản lý danh mục môn học | Quản trị viên | Desktop |
| E | 24 | Quản lý lớp và thành viên | Quản trị viên; bổ sung quyền giảng viên trong môn được giao | Desktop; Web giảng viên theo DECISIONS.md |

### 9.2. Quy tắc nghiệp vụ lõi (3.2.6)

- Trắc nghiệm **một đáp án đúng**; **một lượt mỗi đợt**; thang điểm **10**; câu sai/bỏ trống 0 điểm, không trừ điểm.
- Đề đã mở thi và **snapshot** lượt thi là bất biến.
- Dùng **giờ máy chủ**; mất mạng không kéo dài thời gian; chỉ dữ liệu máy chủ đã xác nhận mới được chấm.
- Khung nhận lượt mới là [OpensAt, ClosesAt); lượt đã bắt đầu được làm đủ DurationMinutes theo Deadline cá nhân. Công bố được bật sau kích hoạt; thí sinh chỉ xem khi bài của mình đã kết thúc và có kết quả.
- Quyền tạo lượt dựa trên SessionClasses và ClassStudents; không sao chép roster mới sang SessionCandidates. Thay đổi thành viên không xóa lượt thi và kết quả đã có.
- Thí sinh chỉ xem điểm sau khi công bố; xem bài và đáp án đúng theo chính sách công bố.
- Không trả đáp án đúng xuống trình duyệt trong lúc làm bài.
- Phân quyền theo vai trò **và** phạm vi dữ liệu (sở hữu bài, môn được phân công, đợt phụ trách); quản trị tài khoản không mặc nhiên có quyền xem đáp án hay sửa kết quả.

### 9.3. Yêu cầu phi chức năng (3.2.5, điều kiện nghiệm thu đề xuất)

| Mã | Nội dung |
|---|---|
| NFR-01 | Bảo mật: kiểm quyền ở máy chủ, HTTPS, băm mật khẩu, chống CSRF/XSS, truy vấn tham số hóa |
| NFR-02 | Toàn vẹn: đồng thời vẫn một lượt/một kết quả; giao dịch, kiểm soát xung đột |
| NFR-03 | Thời gian và phục hồi: giờ máy chủ; tác vụ quét lượt quá hạn (đề xuất 5 giây) |
| NFR-04 | Hiệu năng: 50 thí sinh đồng thời; p95 lưu đáp án ≤ 2 giây (mốc đề xuất, cần ghi rõ cấu hình đo) |
| NFR-05 | Dễ sử dụng: Web từ 360 px, điều hướng bàn phím, trạng thái không chỉ dựa vào màu, nút nộp có xác nhận |
| NFR-06 | Vận hành: nhật ký có mã yêu cầu, không lộ đáp án/token, sao lưu, README dựng và kiểm thử |

---

## 10. Dữ liệu: yêu cầu và các điểm cần bổ sung

**[Đề]** CSDL phải thể hiện đúng yêu cầu đề tài, **đạt 3NF**, dùng chung cho Desktop và Web, triển khai trên SQL Server.

**[Nhóm]** Mô hình hiện hành xem [SCHEMA.md](SCHEMA.md) và migrations EF Core. Các bảng lớp gồm Classes, ClassStudents, SessionClasses; SessionCandidates giữ dữ liệu cũ để đối chiếu. Các bảng snapshot lưu nội dung đã giao cho lượt thi.

**Các điểm cần đối chiếu trong thiết kế và nghiệm thu** (không phải danh sách khẳng định còn thiếu trong code):

| # | Vấn đề | Dữ liệu/quy tắc cần thể hiện |
|---|---|---|
| 1 | Người phụ trách đợt thi (UC-18…21 giới hạn theo phụ trách) | `ExamSessions.ManagedBy` (FK → Users) |
| 2 | Đánh dấu câu hỏi (FR-B14, UC-06) | `Responses.IsFlagged BIT DEFAULT 0` |
| 3 | Trộn đề, quyền xem kết quả (UC-18, UC-08) | `ShuffleQuestions`, `ShuffleOptions`, `ReviewPolicy` ở `ExamSessions` |
| 4 | Trạng thái đợt/lượt và lý do kết thúc | Đợt: Nháp → Kích hoạt → Mở → Đóng, nhánh Hủy/Hoãn. Lượt: Đang làm → Đã kết thúc + `EndReason` (nộp chủ động/hết giờ) + `SubmittedAt`. Quy định điều kiện chuyển trạng thái |
| 5 | Ghi nhận mất kết nối (FR-B26, mở rộng) | `Attempts.LastSeenAt` (cập nhật khi lưu đáp án/heartbeat); máy chủ chỉ biết lần cuối thấy client |

> Mọi thay đổi lược đồ cần được nhóm duyệt trước khi triển khai.

---

## 11. Yêu cầu giao diện và màn hình

**[Đề]** Giao diện rõ ràng, chất lượng tốt, màu sắc phù hợp; Web thân thiện, bố cục hợp lý, nên responsive; Desktop rõ ràng, thao tác thuận tiện, quản lý dữ liệu hiệu quả.

**[Nhóm]** Yêu cầu cho từng nhóm màn hình:

| Màn hình | Yêu cầu chính |
|---|---|
| Theo vai trò | Thí sinh và Giảng viên dùng Web; Quản trị viên dùng Desktop, đúng quyền từng UC |
| Làm bài (Web) | Thời gian còn lại; đánh dấu câu; trạng thái lưu (đang lưu / thành công / thất bại); tiếp tục lượt cũ; xác nhận nộp |
| Tổ chức thi (Web giảng viên) | Thể hiện Nháp, Kích hoạt, Mở, Đóng, Hủy; khóa thao tác theo trạng thái |
| Kết quả (Web) | Phân biệt chưa thi / chờ chấm / chưa công bố / đã công bố; chỉ xem bài, đáp án theo chính sách |
| Liên kết UC | Mỗi màn hình ghi rõ phục vụ UC nào; có thông báo lỗi và trạng thái dữ liệu rỗng ứng với AF/EF |

---

## 12. Điều kiện chặn điểm (trần điểm) và cách tránh

| Trường hợp [Đề] | Điểm tối đa | Biện pháp [Nhóm] |
|---|---|---|
| Không có ứng dụng Web | 6,0 | Ưu tiên Web thí sinh chạy trọn luồng thi |
| Không có ứng dụng Desktop | 6,0 | Ưu tiên Desktop đăng nhập + CRUD + tổ chức thi |
| Web và Desktop không dùng chung dữ liệu nghiệp vụ | 7,0 | Một lõi dịch vụ và một CSDL; demo thao tác ở Desktop rồi thấy ở Web |
| Không dùng SQL Server | 6,5 | Dùng SQL Server ngay từ đầu (không dùng CSDL tạm khác) |
| Sản phẩm không chạy/demo được | 5,0 | Chốt bản demo ổn định sớm, có dữ liệu mẫu và README dựng |
| Chỉ có giao diện, chưa xử lý chức năng chính | 4,0 | Làm luồng nghiệp vụ chính trước giao diện đẹp |
| Không nộp mã nguồn | 4,0 | Nộp đủ mã nguồn + script CSDL |

---

## 13. Yêu cầu demo và vấn đáp

**[Đề]** Demo được Web, Desktop và **sự liên kết dữ liệu**; mỗi sinh viên giải thích được phần việc, mã nguồn, kiến trúc chung; điểm từng SV có thể khác nhau theo đóng góp.

**Kịch bản demo xuyên suốt đề xuất [Nhóm]:**

1. Quản trị viên đăng nhập Desktop → tạo tài khoản, nhập danh sách thí sinh, phân công giảng viên theo môn (UC-14…16).
2. Giảng viên đăng nhập Web → thêm câu hỏi (UC-10) → tạo đề, xác nhận sẵn sàng (UC-11).
3. Quản trị viên quản lý lớp/thành viên trên Desktop (UC-24); giảng viên trên Web gán lớp cùng môn và kích hoạt đợt (UC-18, 19).
4. Thí sinh đăng nhập Web → xem đợt thi → làm bài, đánh dấu câu, thử tải lại trang → nộp (UC-05…07).
5. Giảng viên trên Web theo dõi tham gia (UC-12) → bật công bố kết quả (UC-21); mỗi bài đã kết thúc và chấm xong được xem theo chính sách.
6. Thí sinh trên Web xem điểm, bài, đáp án theo chính sách; xem lịch sử (UC-08, 09).
7. Minh họa kiểm soát: truy cập chéo bài bị chặn, lưu sau hạn bị từ chối, đáp án đúng không có trong dữ liệu trang thi.

---

## 14. Phân công và giải trình cá nhân

**[Đề]** Tất cả sinh viên phải trực tiếp tham gia xây dựng và giải thích được cả phần riêng lẫn kiến trúc chung.

**[Nhóm] đề xuất:** lập bảng phân công cho 4 thành viên (Desktop giao diện, Web giao diện, nghiệp vụ/API, CSDL/kiểm thử), nhưng **mọi thành viên đọc và giải thích được kiến trúc chung** (tầng dữ liệu, tầng nghiệp vụ, luồng Web–API–SQL Server). Chuẩn bị trước các câu hỏi vấn đáp: vì sao tách lớp, vì sao dùng giờ máy chủ, vì sao snapshot, vì sao 3NF, vì sao Desktop gọi API thay vì nối thẳng CSDL.

---

## 15. Điểm cần quyết định và rủi ro

| # | Vấn đề | Lựa chọn | Ghi chú |
|---|---|---|---|
| 1 | Hạn nộp và hình thức nộp | Hỏi giảng viên | Đề không nêu |
| 2 | Khai báo mã do AI hỗ trợ | Hỏi giảng viên | Liên quan quy tắc trích dẫn |
| 3 | UC-17 thuộc vai trò nào | Quản trị viên trên Desktop (đã chốt) | Theo DECISIONS.md |
| 4 | WinUI 3 hay WinForms | WinForms (bắt buộc) | WinUI 3 chỉ là điểm cộng nếu dư năng lực |
| 5 | MVC hay Razor Pages/Blazor | ASP.NET Core MVC (đã chọn) | Phù hợp tách lớp và giải thích |
| 6 | Mức ưu tiên mở rộng | Sau khi bắt buộc ổn định | Tránh vướng trần "không chạy được" |
| 7 | Phiên bản .NET/SQL Server | .NET/EF Core 10; SQL Server 2022 Docker | Mọi thành viên phải dựng lại và chạy được |
| 8 | Mốc đo hiệu năng (NFR-04) | Chỉ ghi số đo thật | Không công bố khi chưa đo |

---

## 16. Danh mục nghiệm thu (Definition of Done)

**Bắt buộc**
- [ ] Web và Desktop chạy được, dùng chung một CSDL SQL Server.
- [ ] Desktop: đăng nhập Administrator; xem/thêm/sửa/xóa (hoặc ngừng sử dụng)/tìm kiếm; Web kiểm tra quyền Student/Lecturer và phạm vi dữ liệu.
- [ ] Web: trang chủ, menu, danh mục đợt thi, tìm kiếm, chi tiết, làm bài, nộp, kết quả, lịch sử.
- [ ] CSDL đạt 3NF, có script triển khai và dữ liệu mẫu.
- [ ] Luồng nghiệp vụ chính chạy trọn: quản trị trên Desktop, giảng viên tổ chức/công bố trên Web, thí sinh thi/xem kết quả trên Web.
- [ ] Mã nguồn tách lớp; xử lý ngoại lệ và validation; README dựng/chạy.
- [ ] Báo cáo đủ 9 mục bắt buộc, có sơ đồ, CSDL, hình minh họa, tài liệu tham khảo.

**Kiểm thử nghiệp vụ trọng yếu**
- [ ] Hai yêu cầu bắt đầu đồng thời → đúng một lượt thi.
- [ ] Lưu đáp án tại/sau hạn bị từ chối.
- [ ] Dữ liệu trang làm bài không chứa đáp án đúng.
- [ ] Thí sinh A không mở được bài của thí sinh B.
- [ ] Nộp hai lần → đúng một kết quả.
- [ ] Chưa công bố thì thí sinh không thấy điểm.
- [ ] Giảng viên chỉ truy cập môn được phân công và đợt mình phụ trách; quản trị tài khoản không tự có quyền xem đáp án hoặc sửa kết quả.

**Điểm cộng**
- [ ] xUnit cho quy tắc nghiệp vụ; thống kê/xuất kết quả; ít nhất một mở rộng khác (Excel, ma trận đề, giám sát mất kết nối).
