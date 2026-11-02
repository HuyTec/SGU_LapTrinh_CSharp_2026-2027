# Tài liệu hiện hành

Báo cáo tổng hợp demo ngày 24/10/2026, vị trí mã nguồn, vận hành và cải thiện: [xem báo cáo](BAO_CAO_DEMO_2026-10-24.md).

[Hướng dẫn vận hành dự án](HUONG_DAN_VAN_HANH.md): cài đặt lần đầu, mở Web/Desktop, tài khoản, quy trình thi, demo, kiểm thử, sao lưu và xử lý lỗi.

1. `YEU_CAU_DU_AN_DE_TAI_18.md`: tiêu chí nghiệm thu chính theo lựa chọn của người dùng.
2. `HỆ THỐNG QUẢN LÝ THI TRẮC NGHIỆM.md`: chi tiết nghiệp vụ và UC. Các quyết định ngày 24/10/2026 bổ sung tại `../DECISIONS.md`; hợp đồng thực tế tại `../CONTRACT.md`.
3. `../README.md`: dựng và chạy sản phẩm; `../REPORT.md`: bằng chứng kiểm tra thực tế và các phần còn thiếu.
4. `CSharp.drawio.xml`: sơ đồ nguồn chỉnh sửa được; cần phân biệt kiểm tra cấu trúc XML với kiểm tra bố cục trực quan.

Bản DOCX và `../MVP_DESIGN.md` là tài liệu phân tích gốc/lịch sử. Không suy ra tính năng đã triển khai từ những tài liệu đó. Báo cáo nộp cuối cần cập nhật từ kết quả nghiệm thu, hình chụp và phân công thật.

## Chọn giao diện

WinForms dùng control chuẩn, bố cục thanh điều hướng, thanh tìm kiếm, bảng chỉ đọc và hộp thoại chỉnh sửa. Web dùng Razor và Bootstrap cục bộ; nền sáng, chữ Segoe UI, nhãn tiếng Việt. Màn thi luôn ưu tiên câu hỏi, thời gian và trạng thái lưu. Không thêm animation hoặc công cụ frontend nặng khi không phục vụ thao tác.

## Tài liệu bên ngoài đã tham khảo

- [Microsoft ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/mvc/overview?view=aspnetcore-10.0).
- [Microsoft Identity model customization](https://learn.microsoft.com/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0): ánh xạ Identity vào bảng nghiệp vụ.
- [Microsoft EF Core SQL Server provider](https://learn.microsoft.com/ef/core/providers/sql-server/).
- [Microsoft JWT bearer configuration](https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).
- [Bootstrap 5.3.8](https://github.com/twbs/bootstrap/tree/v5.3.8), MIT; CSS và LICENSE được lưu cục bộ trong Exam.Web/wwwroot/lib/bootstrap.
- [xUnit](https://xunit.net/): kiểm thử quy tắc và tích hợp SQL Server.

Các JWT tự cấp trong sản phẩm phục vụ mô hình nội bộ môn học đã duyệt, không được mô tả là hệ thống OAuth/OIDC cho nhà cung cấp bên ngoài.
