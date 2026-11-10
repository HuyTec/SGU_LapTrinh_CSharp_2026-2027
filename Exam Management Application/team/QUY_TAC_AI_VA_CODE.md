# Quy tắc làm việc và dùng AI

Áp dụng cho bốn thành viên từ 10/11/2026. Đọc cùng AGENTS.md, DECISIONS.md và CONTRACT.md. Luật này kiểm soát thay đổi; không tự bật branch protection trên dịch vụ Git.

Mã người phụ trách: TV1 = Phùng Anh Vũ (tài khoản), TV2 = Nguyễn Hoàng Phúc (lớp/đề thi), TV3 = Trần Nhựt Huy (Web dự thi/chấm), TV4 = Bùi Nhật Hào (đợt thi). Trách nhiệm ngang bên dưới là đề xuất review. Chấm điểm thuộc service phía server do Huy phụ trách; công bố do Hào phụ trách. Quản lý lớp là module mở rộng theo `team/QUAN_LY_LOP_HOC.md`, chưa tự thêm role, endpoint hoặc migration. Vũ sở hữu auth cả Web/Desktop; Huy phối hợp ghép màn Web và review layout.

**Phạm vi vai trò đã chốt ngày 10/11/2026:** người dùng yêu cầu bỏ tác nhân/role ExamOfficer; Giảng viên nhận quản lý môn, lớp học phần và toàn bộ quy trình đợt thi (tạo/kích hoạt, chọn thí sinh, theo dõi, công bố, xuất) trong phạm vi được phân công. Còn ba vai trò đích: Thí sinh, Giảng viên, Quản trị viên. Mã hiện hành vẫn có ExamOfficer và chưa được sửa; D25 theo dõi chuyển role, quyền và dữ liệu. D26 theo dõi quản lý lớp. Không mô tả chức năng đích như hành vi đã triển khai; team bốn thành viên vẫn là bốn người, độc lập với số vai trò.

## Trước khi code

1. Chọn một mã việc trong Excel. Ghi người thực hiện, reviewer, ngày và trạng thái; đọc code thực tế của module trước khi yêu cầu AI sửa.
2. Viết luồng UI → route/controller → service → bảng SQL → kết quả hiển thị. Nêu vai trò, phạm vi dữ liệu và tiêu chí hoàn thành.
3. Chốt danh sách tệp được sửa. Chủ module được sửa xuyên tầng cho phần việc đó; người phụ trách ngang review phần dùng chung. Không coi module sở hữu cả tệp lớn.
4. Tạo nhánh `codex/<ma-viec>-<mo-ta>` từ nhánh tích hợp nhóm đã thống nhất. Không force-push nhánh dùng chung. Mỗi PR chỉ một mục tiêu có thể nghiệm thu.

## Luật giao diện và shell

| Phần | Người quản lý | Ràng buộc |
|---|---|---|
| MainForm, NavigationService, Theme, Controls | TV1 | Header/sidebar/breadcrumb/đăng xuất dùng chung; tab không tạo shell riêng |
| ResourceView, ResourceEditorForm, ResourcePresenter, ExamApiClient | TV1 | Tệp dùng chung nhiều vai trò; một PR sửa tại một thời điểm |
| _Layout.cshtml, site.css, site.js | TV3 | Giữ layout, màu, thông báo nhất quán; CSS module có phạm vi rõ |
| Contracts.cs, ApiControllers.cs, Program.cs, AdminService.cs | TV2 | Ghi rõ method/resource bị tác động; đổi DTO phải cập nhật hai client và CONTRACT |
| Entities, DbContext, migrations, solution và script tích hợp | TV4 | Chỉ một người sinh migration sau khi schema được thống nhất; không reset DB chung |

Tab phải có loading, rỗng, validation, lỗi mạng, hết phiên và thao tác thành công. Chặn bấm lặp lúc lưu; dùng async/await, không `.Result`/`.Wait()` trên UI. Dispose timer/subscription khi chuyển màn hình. Nhãn tiếng Việt; giữ Segoe UI/system và màu hiện tại trong Theme/site.css. Một người làm tab không tự sửa toàn bộ theme.

Không chia nút “Lưu” và nghiệp vụ lưu cho hai người mà không có một người chịu trách nhiệm đầu cuối. Không tạo bốn API client, bốn menu theo vai trò hoặc bốn bộ DTO trùng nhau.

## Luật nghiệp vụ và dữ liệu

1. Server quyết định quyền, phạm vi, hạn thi, điểm và công bố. Ẩn nút chỉ là UX; API vẫn phải từ chối truy cập sai. Không gửi đáp án đúng xuống màn đang thi.
2. UI không truy vấn SQL; React (nếu bổ sung) cũng chỉ gọi HTTP backend C#. Cả Desktop và Web dùng cùng Exam.Services/Exam.Data và SQL Server.
3. Giữ rowversion khi sửa câu hỏi, xử lý 409 bằng yêu cầu tải lại. Giữ snapshot và cơ chế nộp/chấm idempotent; không tự tính lại giờ thi ở client.
4. Không đưa mật khẩu, JWT, connection string thật, dữ liệu thí sinh vào prompt, log, commit hoặc ảnh. Rà OWASP liên quan đến thay đổi: phân quyền đối tượng, injection, XSS/CSRF, xác thực và secret; không thêm checklist không liên quan.
5. Không thêm React, package, đổi auth, DB provider hoặc tái cấu trúc diện rộng chỉ vì AI đề xuất. Ghi thay đổi kiến trúc và thống nhất với nhóm trước khi triển khai phần phụ thuộc.

## Prompt chung để giao một việc cho AI

```text
Bạn hỗ trợ dự án Exam Management Application (.NET 10).
Đọc AGENTS.md, DECISIONS.md, CONTRACT.md, team/QUY_TAC_AI_VA_CODE.md và code liên quan.
Mã việc: [Dxx]. Người làm: [TVx]. Reviewer: [TVy].
Mục tiêu: [hành vi trước/sau, vai trò và tab].
Tệp/method được sửa: [danh sách chính xác].
Phụ thuộc đã sẵn sàng: [mã việc, route/DTO].
Tiêu chí nghiệm thu: [happy path, validation, sai quyền/phạm vi, lỗi mạng].
Đầu tiên giải thích luồng UI → controller → service → SQL bằng sơ đồ ngắn.
Đưa kế hoạch nhỏ, sau đó thực hiện đúng phạm vi đã được giao.
Không giả định endpoint hoặc component tồn tại; tìm trong source trước.
Nếu cần sửa tệp chung ngoài phạm vi, nêu diff dự kiến và người cần phối hợp.
Không overwrite code người khác, không tắt kiểm thử/authorization để làm xanh build.
Kết thúc: tệp thay đổi, lý do, lệnh đã chạy và kết quả thật, phần chưa kiểm tra.
Giải thích một luồng request cụ thể để tôi có thể tự trình bày và bảo trì.
```

## Prompt kiểm soát code và học luồng

```text
Review diff của mã việc [Dxx], chưa sửa code.
Truy vết thao tác [ví dụ: Giảng viên sửa câu hỏi] từ UI tới DB.
Kiểm tra phạm vi môn, version token, lỗi 401/403/409, bất đồng bộ và dữ liệu trả về.
Chỉ báo lỗi có đường tái hiện hoặc bằng chứng source, kèm file/method và mức ảnh hưởng.
Phân biệt lỗi đã xác nhận, nghi ngờ cần test và gợi ý cải thiện.
Sau review, hỏi tôi giải thích một điểm cốt lõi; mỗi lần một câu, không dồn lý thuyết.
```

## Cổng ghép code

1. Tác giả tự đọc diff, build phần liên quan, chạy test phù hợp, cập nhật tài liệu hợp đồng khi có thay đổi.
2. Reviewer khác tác giả kiểm tra code và bằng chứng. Thay đổi auth/schema cần thêm review ngang của TV2/TV4. Không đồng thời sửa cùng method trong AdminService hay cùng shared control.
3. PR ghi: vấn đề, hành vi mới, mã việc, file dùng chung, test đã chạy, test chưa chạy, ảnh UI nếu đổi màn hình. Không dùng ảnh demo cũ làm bằng chứng mới.
4. Chỉ chuyển Hoàn thành khi có reviewer, bằng chứng và ngày hoàn thành thật; không tự suy ra từ tỷ lệ hoặc build xanh. Ghi đóng góp thực tế, nguồn thư viện/code thích nghi và phần AI hỗ trợ.
5. Trên remote Git, người quản lý repo cấu hình required review và required CI khi đã xác định đúng repository; lượt này chưa có remote hợp lệ để thiết lập.

## Mẫu PR

```markdown
Mã việc / tác giả / reviewer:
Vấn đề và hành vi sau thay đổi:
Luồng và tệp chung bị tác động:
Kiểm tra đã chạy (lệnh + kết quả):
Chưa kiểm tra / hạn chế:
Bằng chứng UI/API/DB và cập nhật hợp đồng:
```
