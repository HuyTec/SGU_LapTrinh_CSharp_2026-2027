# Điều phối nhóm 4 thành viên

> **Phạm vi nghiệp vụ mới 10/11/2026:** theo yêu cầu người dùng, bỏ vai trò Cán bộ quản lý thi; Giảng viên nhận môn, lớp học phần và các nghiệp vụ đợt thi trong phạm vi môn được phân công. Thí sinh và Quản trị viên vẫn giữ. Code hiện còn role `ExamOfficer`; xem `DECISIONS.md`, D25–D28 và `bao_cao/README.md` để theo dõi việc chuyển đổi. Bản phân công cập nhật chia 4 lát dọc: Hào quản lý lớp Admin Desktop, Vũ thí sinh/membership, Phúc đề thi giảng viên Web, Huy dự thi và chấm. Tệp gốc được giữ nguyên.

Ngày rà soát: 10/11/2026. Tên và module đã được người dùng cung cấp; đây chưa phải ghi nhận đóng góp đã hoàn thành. Mã TV1–TV4 giữ ổn định; lịch 11–24/11/2026 vẫn là đề xuất vì chưa có hạn nộp.

1. Mở `PHAN_CONG_NHOM_2026-11-10.xlsx`: xem `Ma tran ngang`, sửa tên tại `Thanh vien`, theo dõi việc tại `Cong viec doc`.
2. Đọc [quy tắc và prompt AI](QUY_TAC_AI_VA_CODE.md) trước khi sửa code.
3. Đọc [kết quả kiểm tra và luồng hệ thống](RA_SOAT_VA_LUONG.md).
4. Làm theo [hướng dẫn Docker và React](DOCKER_REACT_SQLSERVER.md) sau khi xác định đúng nguồn React.

## Hai chiều phân công

**Chiều ngang:** TV1 phụ trách chuẩn Desktop/shell; TV2 phụ trách API/nghiệp vụ; TV3 phụ trách Web; TV4 phụ trách dữ liệu/tích hợp. Đây là trách nhiệm review xuyên suốt, không có nghĩa mỗi người chỉ làm một tầng.

**Chiều dọc:** mỗi người sở hữu module từ giao diện đến API, dữ liệu và kiểm thử. Vũ: tài khoản/quyền của mọi vai trò; Phúc: lớp và đề thi; Huy: quá trình thi/chấm; Hào: đợt thi. Một module có thể xuất hiện trên nhiều vai trò, không phân chia độc quyền cả vai trò cho một người. Mọi thay đổi tầng dùng chung được người phụ trách ngang review.

“Cell hệ thống” được hiểu là **shell dùng chung** (header, sidebar, breadcrumb, thông tin phiên, thông báo) và **vùng nội dung thay đổi theo vai trò/tab**. Không chia từng ô/header thành bốn cách cài đặt khác nhau.

| Mã                         | Phụ trách ngang                      | Module dọc                                                                  | Reviewer chính |
| --------------------------- | -------------------------------------- | ---------------------------------------------------------------------------- | --------------- |
| TV1 — Phùng Anh Vũ       | Desktop, shell, quyền tài khoản     | Tài khoản mọi vai trò, login/đăng ký/duyệt, phân công giảng viên | TV2             |
| TV2 — Nguyễn Hoàng Phúc | API, DTO nghiệp vụ                   | Lớp, chủ đề, ngân hàng câu hỏi, đề thi                             | TV4             |
| TV3 — Trần Nhựt Huy      | Web, layout, trải nghiệm dự thi     | Đợt được giao, làm bài, chấm tự động server, kết quả            | TV1             |
| TV4 — Bùi Nhật Hào      | SQL, migration, tích hợp, cấu hình | Môn học, đợt thi, phân công thí sinh, công bố/xuất                 | TV3             |

Ước lượng trong Excel là giờ làm tập trung, không phải cam kết. Mỗi người có 28 giờ thực hiện/thiết kế và 4 giờ review dự kiến. Riêng D08 của Phúc là thiết kế lớp (8 giờ), chưa bao gồm triển khai UI/API/DB; phải bổ sung ước lượng khi chốt phạm vi. Xem [đề xuất quyền và luồng lớp học](QUAN_LY_LOP_HOC.md). Workbook là nguồn phân công chi tiết, README giữ vai trò tổng quan.

Các mã A1/A2/A3 trong CONTRACT.md là phân vai tác nhân của đợt triển khai cũ, không phải tên bốn sinh viên. Kế hoạch này bổ sung cách điều phối con người, không thay đổi hợp đồng API hoặc kiến trúc đã duyệt.

Excel hiện có 16 việc. Số việc hoàn thành chỉ đếm khi trạng thái Hoàn thành có reviewer khác tác giả, bằng chứng và ngày thực tế; nội dung bằng chứng vẫn cần người review xác nhận. Nếu thêm dòng ngoài 16 việc, mở rộng phạm vi công thức trên sheet Thanh vien, validation và conditional formatting tương ứng. Ngày là giá trị Excel thật. Đã kiểm tra công thức đổi theo giờ và điều kiện hoàn thành, render ba sheet; chưa mở thử bằng Excel/WPS native.

# NHỮNG ĐIỂM CẦN ĐIỀU CHỈNH

> **Ghi chú rà soát đầu vào ngày 10/11/2026:** các điểm bên dưới là nhận xét cần đối chiếu, không thay thế quyết định đã duyệt. Xem [báo cáo và kết quả hiệu chỉnh](bao_cao/README.md). Theo phạm vi mới, role đích ExamOfficer bị loại và giảng viên nhận UC-12/13/17–21 cùng quản lý lớp; Identity, AuthSessions, Web API và xuất CSV/XLSX vẫn giữ theo yêu cầu/kiến trúc. Mã nguồn chưa được chuyển role. Bản phân công mới bổ sung D25–D27 nằm trong `PHAN_CONG_NHOM_2026-11-10_CAP_NHAT.xlsx`; tệp gốc đang mở trong WPS nên được giữ nguyên.

## MỨC 1: Nghiêm trọng (sửa trước khi code lại)

| # | Vấn đề                                                                                                                                                                                                                                          | Cần sửa                                                                                                                                                                                         |
| - | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1 | **Thiếu hẳn FR nhóm D và E.** Bảng 3.2.1 nêu nhóm D và E, nhưng mục 3.2.2.1 chỉ có A, B, C. US-08, 09, 10, 14, 15 lại tham chiếu FR-D01 đến D07 và FR-E01 đến E03, những mã không tồn tại.                           | Viết đủ FR-D (đợt thi, theo dõi, thống kê, công bố, môn học) và FR-E (tài khoản, thí sinh, phân công giảng viên). Nếu không, người/AI dựng code sẽ tự bịa phần này. |
| 2 | **Ràng buộc "tổng điểm đề = 10" (FR-C14, UC-11.3, US-07).** Đề 3, 6, 7, 9, 11, 30 câu đều không chia đều được với DECIMAL(5,2) (ví dụ 3 câu × 3,33 = 9,99), nên hệ thống sẽ chặn xác nhận hầu hết các đề. | Lưu trọng số điểm mỗi câu, không ép tổng. Khi chấm: `Score = SUM(điểm câu đúng) / SUM(điểm cả đề) × 10`, làm tròn 2 chữ số. Bỏ điều kiện "tổng = 10".            |
| 3 | **BR11 mâu thuẫn UC-18.** BR11 khóa sau khi *mở*, UC-18 và bảng trạng thái khóa ngay sau khi *kích hoạt*. Báo cáo tự nhận ra mâu thuẫn ở mục 3.3.3 nhưng không sửa.                                                | Sửa BR11 thành "khóa từ khi kích hoạt". Đây là mốc khóa đúng.                                                                                                                        |
| 4 | **BR12 (không giao lịch) định nghĩa sai.** Nó so sánh *cửa sổ mở* (StartsAt đến EndsAt) chứ không phải thời lượng làm bài. Cửa sổ 8h–17h với bài 45 phút sẽ chặn thí sinh có hai môn cùng ngày.           | Hoặc dùng giờ bắt đầu cố định (cửa sổ = thời lượng), hoặc chỉ cấm hai lượt InProgress đồng thời.                                                                            |
| 5 | **"Thu hồi phiên" không có cơ chế.** UC-02, 04, 14 yêu cầu thu hồi phiên, nhưng WinForms dùng token có thời hạn (stateless) và 17 bảng không có bảng phiên/refresh token.                                               | Thêm cột `TokenVersion` (hoặc `SecurityStamp`) vào Users, kiểm tra mỗi request. Hoặc bỏ yêu cầu thu hồi.                                                                           |
| 6 | **Identity và bảng tự định nghĩa xung đột nhau.** Báo cáo vừa đề xuất ASP.NET Core Identity, vừa tự có Users (PasswordHash), Roles, UserRoles trong 17 bảng. Identity có bộ bảng riêng.                                | Chọn một. Gọn nhất: giữ Users/Roles/UserRoles, dùng `PasswordHasher` + JWT, bỏ Identity đầy đủ.                                                                                      |
| 7 | **Chấm lỗi dẫn đến công bố bị kẹt.** UC-21 yêu cầu mọi lượt đã chấm, nhưng "sửa điểm thủ công" nằm ngoài phạm vi. Lượt `Failed` không có ai thử lại nên đợt không bao giờ công bố được.          | Thêm job tự chấm lại các lượt Failed/Pending, hoặc cho công bố kèm cảnh báo.                                                                                                         |
| 8 | **Hình 3.4.12 ghi "giới hạn lượt thi"; quy trình d ghi "hết số lượt cho phép".** Thiết kế chỉ có đúng 1 lượt (UNIQUE).                                                                                                    | Xóa mọi nhắc đến "số lượt cho phép".                                                                                                                                                     |

## MỨC 2: Cao (mâu thuẫn phân quyền và truy vết)

- **Bảng đầu vào/đầu ra (3.1.2)** ghi "Cán bộ quản lý thi" cung cấp câu hỏi, đáp án, cấu trúc đề. Sai, đó là việc của giảng viên.
- **UC-17 (danh mục môn học)** thuộc cán bộ theo bảng 3.1.3 và US-14, nhưng bảng 3.2.1 xếp vào nhóm E của quản trị viên. Nên chuyển sang Quản trị viên: toàn bộ dữ liệu gốc (tài khoản, thí sinh, môn, phân công) về một chỗ, logic hơn.
- **Cán bộ chọn đề thi:** cần ghi rõ cán bộ chỉ thấy tên, môn, số câu, không thấy nội dung hay đáp án (nguy cơ lộ đề).
- **Map User Story sang FR sai lệch.** Ví dụ US-03 trỏ FR-B02/B03 (lẽ ra là B11 đến B15), US-04 trỏ B04 (lẽ ra B16 đến B22), US-05 trỏ B05 (lẽ ra B23 đến B25), US-07 trỏ C04/C06 (lẽ ra C11 đến C16), US-11 trỏ A03/A04 (lẽ ra A06/A07), US-13 trỏ A02 (lẽ ra A05), US-12 trỏ C03/C05 (lẽ ra C09/C13).
- **Ảnh minh họa không khớp đặc tả.** Hình 3.4.2 có "duyệt đăng ký" (không có UC nào), Hình 3.4.1 "chọn vai trò làm việc" (không có trong FR-A01), Hình 3.4.8 xuất CSV/XLSX trong khi ClosedXML mới chỉ "cân nhắc". Ngoài ra mục 2.7.3 nói "chưa có dự án thực tế" nhưng 3.4.3 lại có ảnh chụp từ dự án. Đây là dấu hiệu code và spec đã lệch nhau.
- **UC-12 và FR-B26 tự mâu thuẫn.** UC-12/AF-02 ghi "không suy diễn là mất mạng", FR-B26 lại bắt hiển thị mất kết nối.
- **Hai kiểu người dùng một tài khoản:** FR-A02 ép thí sinh dùng Web, còn lại dùng Desktop, nhưng 3.1.3 và 3.2.5 cho phép nhiều vai trò trên một tài khoản. Cần nói rõ khi tài khoản vừa là thí sinh vừa là giảng viên thì đăng nhập thế nào.

## MỨC 3: Trung bình (lỗi kỹ thuật thiết kế)

- **FK ghép của Responses** `(AttemptQuestionID, AttemptOptionID) → AttemptOptions` cần một UNIQUE trên cặp cột này ở AttemptOptions. Báo cáo chỉ khai PK là AttemptOptionID nên SQL Server sẽ từ chối.
- **Không có RowVersion/concurrency token ở bảng nào**, trong khi UC-10.2 và NFR-02 yêu cầu "kiểm tra phiên bản/xung đột".
- **Exams.ExamStatus không định nghĩa miền giá trị** (Draft/Ready?), trong khi ExamSessions thì có.
- **Không có UC "nhân bản đề"** dù BR09 yêu cầu "tạo đề hoặc phiên bản mới" khi đề đã khóa.
- **Ba cờ ShowScore/AllowAnswerReview/ShowCorrectAnswers độc lập** cho phép tổ hợp vô nghĩa (hiện đáp án nhưng không cho xem bài). Gộp thành một cột enum hoặc thêm CHECK.
- **Trạng thái Open/Closed lưu cứng phụ thuộc job quét 5 giây.** Nên suy ra từ StartsAt/EndsAt, job chỉ ghi nhận.
- **Múi giờ:** DB lưu UTC, người dùng VN nhập giờ địa phương. Chưa có quy ước chuyển đổi, rất hay gây lệch giờ mở/đóng.
- **Mật khẩu ban đầu của thí sinh** (UC-15 tạo luôn tài khoản) không nói ai đặt và đặt thế nào.
- **"Đủ điều kiện dự thi"** (1.3.2) mơ hồ, trong khi hệ thống chỉ có điều kiện duy nhất là được phân công.
- **Thiếu tác nhân "Hệ thống/Scheduler"** trong use case, dù mở/đóng đợt, hết giờ, chấm lại đều do nó làm.

## MỨC 4: Thấp (trình bày, học thuật)

- **Ghi chú nội bộ lọt vào báo cáo:** "mô hình gốc", "CSDL GỐC 17 bảng", "Quiz amDB 18", tên file `.sql`, "Đối chiếu nguồn… (65 trang)", "Không cần chèn script CREATE TABLE…", "phần mô tả BR11 nên được nhóm chỉnh…". Xóa hết.
- Đoạn rác ".2 mục tiêu cụ thể" ở cuối đoạn 1.1; mục lục thiếu các tiêu đề chương và 3.1.5; mục lục tới trang 83 nhưng ghi "65 trang".
- **Thiếu phần Tài liệu tham khảo**, trong khi có trích dẫn [6], [9].
- **Lỗi chữ:** "use story", "công dụng riêng lẻ" (công cụ), "Giải mã ánh xạ" (giảm mã ánh xạ), "đặt biệt", "sự dụng", "cán bộ quản lý thi cử". Bảng 2.7.2 dòng "Tài khoản" bị copy nhầm mục đích của Web API.
- Thuật ngữ lẫn lộn: "bài làm / bài thi / lượt thi". Chọn một.

## NÊN BỎ

| Bỏ                                                                                                           | Lý do                                                                                                                                                                                                                                       |
| ------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **FR-B26 + 3 cột LastHeartbeatAt/DisconnectedAt/ReconnectedAt**                                        | Là "mở rộng" nhưng đã vào schema lõi, kéo theo heartbeat ghi DB liên tục, và tự mâu thuẫn UC-12.                                                                                                                              |
| **UC-03 (hồ sơ cá nhân), UC-10.4 (Excel câu hỏi), UC-11.2 (sinh đề ma trận)** và US-11, US-12 | Đều gắn nhãn mở rộng nhưng vẫn nằm trong US/wireframe. Đưa hết sang "Hướng phát triển". Bỏ luôn ClosedXML.                                                                                                                 |
| **"Duyệt đăng ký"** (Hình 3.4.2)                                                                   | Không có UC, trái với "tài khoản do đơn vị cấp".                                                                                                                                                                                   |
| **Phân biệt "hoãn" và "hủy" thành luồng riêng**                                                 | Đơn giản hóa: "Hủy kích hoạt (về nháp)" và "Hủy đợt".                                                                                                                                                                           |
| **ExamSessions.CreatedBy hoặc ManagedBy**                                                              | Trùng vai trò, giữ một.                                                                                                                                                                                                                  |
| **Bảng 3.3.5, 3.3.6, 3 trong 4 ví dụ T-SQL, đoạn chuẩn hóa 3NF chung chung**                     | Lặp lại 3.3.3 và 3.3.4, làm báo cáo phình mà không thêm thông tin.                                                                                                                                                                |
| **Web API riêng cho WinForms (cân nhắc)**                                                            | Báo cáo tự nhận đây là phương án đề xuất, không bắt buộc. Nếu nhóm thiếu thời gian, Desktop dùng chung lớp BLL/DAL cũng đáp ứng yêu cầu môn. Đổi lại phải chấp nhận chuỗi kết nối nằm ở máy khách. |

**Giữ lại** UC-04 (đổi mật khẩu), thậm chí nên nâng lên "bắt buộc", vì không có khôi phục qua email và quản trị viên cấp mật khẩu ban đầu.

## Thứ tự nên làm

1. Sửa mục 1 và 2 trong spec: viết đủ FR-D/E, sửa công thức điểm, BR11, BR12, và chốt cơ chế xác thực.
2. Cắt các mục "Nên bỏ" khỏi phạm vi và schema.
3. Sửa schema: UNIQUE trên AttemptOptions, RowVersion, TokenVersion, miền ExamStatus.
4. Dọn trình bày (ghi chú nội bộ, mục lục, tham khảo) cuối cùng.

Nếu bạn cho biết dự án sinh ra sai ở đâu (ví dụ chấm điểm, phân quyền, mở/đóng đợt thi hay đăng nhập), mình sẽ đối chiếu ngược lên đúng mục spec gây ra lỗi đó. Mình cũng có thể viết lại luôn các phần đã nêu (FR-D/E, BR11/12, công thức điểm, bảng trạng thái).
