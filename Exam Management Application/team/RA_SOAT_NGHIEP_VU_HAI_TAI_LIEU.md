# Rà soát nghiệp vụ hai tài liệu nhóm 5

Ngày 10/11/2026. Nguồn: hai file Desktop do người dùng chỉ định, yêu cầu mới về lớp quản lý thí sinh, và code hiện tại. Đây là rà soát nội dung chữ/bảng; chưa xác minh tất cả hình nhúng, sơ đồ và bố cục trang. Không dùng số trang mục lục để suy đoán vị trí. Hai file gốc chưa bị sửa.

## Kết luận

Hai tài liệu chưa đáp ứng nghiệp vụ chọn lớp dự thi vừa được người dùng làm rõ. Đây là khoảng thiếu xuyên suốt từ mô hình dữ liệu, use case đến giao diện, không thể khắc phục bằng đổi nhãn “Thí sinh” thành “Lớp”. Ngoài ra có mâu thuẫn sẵn trong Word về mốc khóa cấu hình. Một số phần còn đúng: giờ server, một lượt mỗi đợt, snapshot bài làm, chấm/công bố riêng, chưa thi không bằng điểm 0. Không kết luận toàn bộ tài liệu sai.

## Các phát hiện và hướng sửa

| Mã / mức | Bằng chứng | Vấn đề | Hướng sửa |
|---|---|---|---|
| F01 / Cao | Word 3.2.3 UC-19, luồng chính bước 1: chọn hồ sơ thí sinh có sẵn; Excel sheet Phân công thí sinh D41: Thí sinh | Luồng chính chọn cá nhân, thiếu chọn lớp theo yêu cầu hiện tại. Đây là thay đổi so với mô hình cũ, không phải thao tác chọn cá nhân tự nó luôn sai | Chọn một/nhiều lớp → xem trước danh sách hợp nhất → kiểm lỗi → xác nhận; xử lý cá nhân chỉ là ngoại lệ nếu nhóm cho phép |
| F02 / Cao | Word 3.3.1–3.3.3 chỉ có Users và SessionCandidates, bảng 3.3.2 giữ 17 thực thể; Excel Cover danh sách màn hình hàng 40–65 | Không có lớp/thành viên lớp hay màn quản lý lớp. Không có nguồn dữ liệu để chọn lớp | Bổ sung khái niệm lớp, quan hệ thí sinh–lớp, UC và màn quản lý lớp/danh sách lớp; sửa ERD, từ điển dữ liệu và truy vết. Không giữ số 17 bảng làm ràng buộc thiết kế |
| F03 / Cao | Word UC-19 chỉ mô tả cặp đợt–thí sinh; Excel Phân công thí sinh AF41/AF43/AF44 | Thiếu quy tắc nạp theo lớp: nguồn danh sách, thời điểm chốt, học viên trùng, chuyển lớp sau chốt, lớp rỗng, tài khoản bị khóa, phạm vi lớp | Membership của lớp là nguồn chọn; SessionCandidates là danh sách dự thi đã xác nhận. Xem trước không ghi dữ liệu; xác nhận kiểm lại trong transaction; kích hoạt khóa danh sách. Không tự đồng bộ lớp vào đợt đã khóa |
| F04 / Cao | Word 3.1.5 BR09/BR11: khóa sau mở; UC-18 bước 4 và UC-19 EF-02: khóa từ kích hoạt; 3.3.3 tự ghi chú mâu thuẫn BR11; Excel Yêu cầu B69 cũng ghi nhận | Hai thời điểm khác nhau cho phép hai cách triển khai/test đối nghịch | Dùng mốc kích hoạt như UC chi tiết đã xác định; chỉ hoãn hợp lệ trước lần mở đầu mới về nháp để sửa; sửa chính BR, không chỉ thêm ghi chú |
| F05 / Vừa | Excel Tổ chức đợt thi D44:R44 gộp Trộn/Chính sách xem kết quả thành CheckBox group/Boolean; Word 3.3.3 định nghĩa ba cờ kết quả; code dùng ReviewPolicy enum | Chưa có một hợp đồng rõ cho cấu hình trộn và quyền xem kết quả; các cách biểu diễn không tương đương tự động | Tách ShuffleQuestions, ShuffleOptions khỏi chính sách kết quả. Chốt dùng ba lựa chọn hiện tại hay các cờ độc lập, quy tắc tổ hợp và ánh xạ; không tự sửa nghiệp vụ theo UI demo |
| F06 / Vừa | Word 3.4.3 mô tả duyệt đăng ký ở hình tài khoản; Excel Đăng nhập Desktop D45 mở Web đăng ký; nhưng Cover/Yêu cầu thiếu màn đăng ký/luồng chờ duyệt, sheet Tài khoản không đặc tả nút duyệt | Đường dẫn vào đăng ký có nhưng thiếu hợp đồng xử lý kết quả đăng ký và quyền duyệt | Bổ sung màn đăng ký, thông báo chờ duyệt và action duyệt; không dùng Mở khóa thay cho Duyệt. Đồng bộ với phần tài khoản Vũ phụ trách |

## Những khác biệt với source cần quyết định, không tự coi source là yêu cầu đúng

Word BR07 và Excel Ngân hàng câu hỏi AF43 thống nhất từ 2 phương án; API/editor demo hiện cố định option1..option4. Đây là chênh giữa đặc tả và triển khai, không phải hai tài liệu mâu thuẫn với nhau. Chốt hỗ trợ số phương án động hoặc sửa phạm vi thành đúng 4, rồi đồng bộ test/UI/DTO.

Word có SessionStatus Draft/Activated/Open/Closed/Cancelled, GradingStatus riêng và các tên DeadlineAt/ResultsPublishedAt. Code dùng trạng thái lưu Active kèm giờ để suy ra mở/đóng, kết quả và các tên khác. Mô hình thiết kế không nhất thiết sai, nhưng cần bảng ánh xạ hoặc kế hoạch refactor; không dùng tài liệu này để khẳng định schema hiện tại giống hệt. Word cũng đã nói mô hình là phương án thiết kế chưa kiểm thử.

Các sheet mở rộng (hồ sơ, đổi mật khẩu, Excel câu hỏi, ma trận đề) được ghi rõ mở rộng. Không nên coi sự có mặt của chúng là bằng chứng đã triển khai hoặc lỗi phạm vi tự thân.

## Nghiệp vụ lớp đã hiểu lại

Lớp là nhóm quản lý thí sinh, ví dụ DCT1231. Không mặc định một lớp chỉ thuộc một môn, không dùng SubjectId bắt buộc để định nghĩa lớp. Người dùng chưa chốt một thí sinh có được thuộc nhiều lớp đồng thời hay cần lịch sử chuyển lớp; phải làm rõ trước schema chính thức.

Luồng: quản lý lớp/thí sinh → tạo đợt và chọn đề Ready → chọn lớp → xem trước danh sách → kiểm quyền, tài khoản, trùng và lịch → xác nhận SessionCandidates → kích hoạt → thí sinh làm bài → chấm → công bố.

Quyền quản lý lớp là đề xuất cần chốt, không tự sinh role mới. Phúc phụ trách code lớp/đề; Hào phụ trách chọn lớp trong đợt; Vũ phụ trách tài khoản/quyền; Huy phụ trách lượt thi/chấm. Việc ai viết code không quyết định actor nào được dùng chức năng.

## Sửa đồng bộ theo thứ tự

1. Word: định nghĩa lớp ở 3.1, ma trận quyền, BR quản lý/chốt lớp và sửa BR09/BR11; bổ sung UC lớp và viết lại UC-19.
2. Word: bổ sung thực thể/quan hệ trong 3.3, quy tắc chuyển lớp và khóa danh sách; sửa sơ đồ, bảng vật lý và các khẳng định số bảng có liên quan.
3. Excel: thêm quản lý lớp/chi tiết lớp; sửa Phân công thí sinh thành phân công theo lớp; bổ sung preview, số lượng hợp lệ/lỗi, xác nhận và trạng thái khóa. Cập nhật Cover/Menu/Yêu cầu và ảnh minh họa bị ảnh hưởng.
4. Chốt chính sách kết quả và số phương án; đồng bộ Word, Excel, CONTRACT và code bằng thay đổi riêng có test.
5. Sau khi nội dung thống nhất mới cập nhật code/migration, chạy integration và thay ảnh demo bằng ảnh đúng phiên bản. Không sửa tài liệu cho khớp code sai chỉ để hết chênh lệch.

## Tiêu chí kiểm tra UC-19 mới

Chọn lớp A có 30 thí sinh và lớp B có 25: preview hiển thị số người duy nhất sau hợp nhất, không mặc định tổng luôn là 55. Có lỗi phải báo theo người/lớp và nêu rõ giao dịch được áp dụng toàn bộ hay chỉ tập hợp đã được người dùng xác nhận; không âm thầm bỏ người lỗi. Khi xác nhận, server kiểm lại dữ liệu vì preview có thể đã cũ. Sau kích hoạt, thay membership không sửa danh sách thi hoặc bài làm đã có.

## Giới hạn

Đã đọc nội dung văn bản của Word và 28 sheet Excel, truy xuất tọa độ ô gốc; chưa rà trực quan toàn bộ hình nhúng. Không sửa hai file Desktop, không cài tool theo MASTER PROMPT đính kèm, không chạy migration. MASTER PROMPT là tài liệu đính kèm chứ chưa phải yêu cầu cài môi trường trong lượt này.
