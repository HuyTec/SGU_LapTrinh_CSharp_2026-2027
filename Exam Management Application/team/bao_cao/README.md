# Báo cáo và đặc tả hiệu chỉnh ngày 10/11/2026

- `BAO_CAO_THI_TRAC_NGHIEM_DA_DIEU_CHINH.docx`: bản rút gọn, 5 bảng; bỏ sơ đồ UC lặp, hình ERD/ảnh demo cũ và ví dụ SQL dư. Use case được tóm tắt; phụ lục ghi 16 nhóm điều chỉnh và việc còn phải kiểm chứng.
- `DAC_TA_USECASE_REPORT.xlsx`: 31 UC gồm 4 UC hoãn, 66 FR, 16 User Story, 16 nhóm đối chiếu. Sheet `Danh muc UC` ghi dòng bắt đầu từng đặc tả ở sheet `Dac ta UC`. Các luồng/tiêu chí là đặc tả, không là bằng chứng kiểm thử thành công.
- `../PHAN_CONG_NHOM_2026-11-10_CAP_NHAT.xlsx`: 28 việc, trong đó D25–D28 là bốn lát chức năng theo kiến trúc mục tiêu; 202 giờ làm + 16 giờ review dự kiến. Đây là ước lượng cần nhóm xác nhận, không ghi nhận thành viên đã hoàn thành. Bản phân công gốc được giữ nguyên.

## Kết luận đối chiếu

Không áp dụng nguyên trạng các nhận xét chưa đối chiếu ở phần cuối README của team. Theo phạm vi đã chốt, Giảng viên thay ExamOfficer tại UC-12/13/17–21; Identity vẫn ánh xạ Users/Roles/UserRoles, AuthSessions, đăng ký/duyệt nhân sự, Web API và CSV/XLSX vẫn thuộc baseline. Mã nguồn hiện có alternate key/FK ghép của Responses, RowVersion của Questions, CHECK Draft/Ready và ReviewPolicy; báo cáo phản ánh chúng thay vì đề nghị tạo trùng.

Điểm cần sửa tiếp: AdminService.ActionAsync nhánh ready còn ép tổng điểm 10, trong khi ExamRules.Score đã chuẩn hóa trọng số; QuestionPointsPicker vẫn hiển thị `/ 10`. D18 xử lý phần này. BR12 hiện kiểm tra giao cửa sổ thi, không phải thời lượng; thay chính sách phải chốt riêng. ExpiryWorker có retry, nhưng D21 cần kiểm chứng lỗi chấm, chốt nộp trước hạn và tính độc lập giữa các lượt trong batch.

## Phạm vi vai trò đã chốt

Người dùng quyết định bỏ tác nhân/role Cán bộ quản lý thi và giao Giảng viên nhận cả tạo/kích hoạt đợt, chọn thí sinh, theo dõi, công bố và xuất kết quả, bên cạnh quản lý môn và lớp học phần. Phạm vi đích còn ba vai trò: Thí sinh, Giảng viên, Quản trị viên. Giảng viên chỉ thao tác trong phạm vi môn được phân công; lớp học phần là nguồn tiện ích để chọn thí sinh, còn danh sách/snapshot thí sinh của lượt thi độc lập sau khi kích hoạt.

Đây là quyết định về phạm vi báo cáo và công việc chuyển đổi, chưa phải mô tả code đã hoàn tất. Source hiện vẫn có `ExamOfficer` trong auth, phân quyền và phạm vi đợt. Workbook cập nhật chia D25–D28 thành bốn lát: lớp/Admin Desktop (Hào), thí sinh và membership (Vũ), đề thi giảng viên/Web (Phúc), dự thi/chấm (Huy).

## Phạm vi thay đổi và kiểm tra

Chỉ sửa tài liệu và phân công; không sửa code ứng dụng, cơ sở dữ liệu hoặc quyết định kiến trúc. Bản DOCX gốc trên Desktop được giữ nguyên.

Đã kiểm tra cấu trúc DOCX/ZIP, nội dung/mã truy vết, 31 khối đặc tả, công thức tổng hợp phân công, điều kiện hoàn thành, validation, bảng lọc và vùng định dạng mới. Workbook phân công hiện có 28 việc, 202 giờ làm, 16 giờ review, 218 tổng giờ theo ước lượng, 0 hoàn thành; chưa mở lại bằng Excel/WPS native.

Chưa kiểm tra dàn trang DOCX bằng renderer: công cụ đi kèm thiếu LibreOffice; Computer Use báo chưa được cấp quyền dùng WPS. Chưa mở lại bằng Excel/WPS để xác nhận hành vi native, chưa chạy build/SQL/API/UI của ứng dụng. D23/D24 giữ các việc kiểm chứng này; không dùng kiểm tra cấu trúc thay cho kiểm tra hình thức hoặc nghiệm thu chức năng.

Nguồn: báo cáo DOCX gốc người dùng cung cấp; team/README.md; DECISIONS.md; docs/YEU_CAU_DU_AN_DE_TAI_18.md; Exam.Services/{AuthService,AdminService,ExamService,ExamRules,ExportService}.cs; Exam.Data/{Entities,ExamDbContext}.cs; Exam.Web/Program.cs và Controllers/StudentControllers.cs; Exam.Desktop/QuestionPointsPicker.cs.
