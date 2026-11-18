# Làm việc nhóm

Mở [bảng phân công](PHAN_CONG_NHOM_THEO_MANG.xlsx), chọn task và xem người làm, reviewer, việc phụ thuộc, file cần sửa và cách nghiệm thu. Người phụ trách mảng ghép các chức năng. Người nhận task làm trọn luồng và ghi bằng chứng.

```powershell
git switch -c codex/ts01-vao-thi-bang-ma
dotnet build '.\Exam Management Application.slnx' -m:1
.\scripts\Start-SqlDocker.ps1
.\scripts\Test-Local.ps1
```

Đổi `ts01-vao-thi-bang-ma` theo task. Trước khi sửa DTO, service, shell hoặc migration dùng chung, thống nhất với các task liên quan. Trong PR ghi mã task, hành vi thay đổi, lệnh kiểm tra và kết quả thực tế. Giữ dữ liệu thật, mật khẩu và token ngoài Git.

Kiến trúc đích: Web Thí sinh và Giảng viên, Desktop Quản trị. Quản trị quản lý môn và lớp; giảng viên tổ chức thi theo môn được giao; server kiểm tra quyền vào thi theo lớp. Workbook ghi rõ việc nào cần chuyển từ code hiện tại. [Tài liệu báo cáo](bao_cao/README.md) · [Lệnh chạy ứng dụng](../README.md).
