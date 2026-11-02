# Thiết kế Desktop: Không gian khảo thí

## Hướng thiết kế đã triển khai

WinForms .NET 10, control chuẩn kết hợp control tự vẽ nhỏ, Segoe UI và icon Windows. MVP nhẹ: View quản lý hiển thị/tương tác, Presenter gọi ExamApiClient; quyền, lưu dữ liệu và chấm điểm vẫn ở backend hiện có. Không thay App1, Web, dữ liệu đang sử dụng hoặc thêm thư viện UI ngoài. Người dùng đã duyệt hướng thiết kế và bản triển khai hiện có trong Exam.Desktop. Bằng chứng render, kích thước và giới hạn kiểm tra nằm tại artifacts/desktop-redesign/qa.md.

Điểm nhận diện là minh họa trang vở/đề thi ở đăng nhập, thanh bên mang dấu hiệu học thuật và quy trình “Chuẩn bị → Tổ chức → Đánh giá”. Bảng dữ liệu là trung tâm làm việc; trang tổng quan có khối hướng dẫn bước tiếp theo, không dùng một dãy số liệu trang trí thay nội dung thực.

## Ba bố cục chính

```text
Đăng nhập
┌─────────────────────────┬────────────────────────────────┐
│ Exam                    │ Chào mừng trở lại              │
│ Không gian khảo thí     │ Tên đăng nhập                  │
│ Minh họa học tập        │ Mật khẩu + hiện/ẩn             │
│ Chuẩn bị                │ Vai trò hoạt động              │
│ Tổ chức                 │ Đăng nhập                      │
│ Đánh giá                │ Máy chủ · Hướng dẫn · Đăng ký  │
└─────────────────────────┴────────────────────────────────┘

Tổng quan
┌───────────┬─────────────────────────────────────────────┐
│ Thu gọn   │ Tổng quan                Tên và vai trò     │
│ Menu theo ├─────────────────────────────────────────────┤
│ vai trò   │ Lời chào, quy trình và thao tác tiếp theo    │
│           │ Số liệu thật theo quyền, có nhãn phạm vi     │
│           │ Danh sách gần đây / tiến độ và phân bố điểm │
│ Hướng dẫn │ Trạng thái tải/rỗng/lỗi và nút thử lại       │
└───────────┴─────────────────────────────────────────────┘

Đợt thi
┌───────────┬─────────────────────────────────────────────┐
│ Sidebar   │ Tổ chức đợt thi                  Tạo đợt    │
│           │ Tìm kiếm · Lọc trạng thái · Làm mới         │
│           │ Bảng tên / đề / lịch / trạng thái           │
│           │ Kích hoạt · Hoãn · Hủy · Công bố · Xuất     │
│           │ Chi tiết lựa chọn, phân trang và hướng dẫn  │
└───────────┴─────────────────────────────────────────────┘
```

Nội dung căn trái, dùng khoảng cách theo bội số 8; căn phải cho hành động cuối biểu mẫu. Thanh bên khoảng 240 px ở 100% DPI, có chế độ thu gọn. Thử ở 1366×768 và 1920×1080, 100% và 150% DPI.

## Token giao diện

| Token | Giá trị / cách dùng |
|---|---|
| Primary / hover | `#1E4E8C` / `#163B6B` |
| Accent | `#2A9D8F`, dùng nét minh họa/tiến độ hoặc nền nhạt, không đặt chữ trắng nhỏ trực tiếp |
| Background / surface / border | `#F5F7FA` / `#FFFFFF` / `#E1E6EC` |
| Text / secondary | `#1F2937` / `#6B7280` trên nền trắng |
| Secondary trên nền trang | `#626D7C` để có thêm khoảng an toàn tương phản |
| Success / warning / error / info | `#2E9E5B` / `#E9A23B` / `#D64545` / `#3B82F6`; badge dùng nền nhạt với chữ đậm tối |
| Danger button | Chữ trắng trên `#B83232`, biểu đạt xóa/hủy bằng nhãn và xác nhận |
| Font | Segoe UI; tiêu đề 18–20 pt, mục 13–14 pt, nội dung 10–11 pt, chú thích 9 pt |
| Góc / khoảng cách | 6–8 px / padding 16–24 px; nhân theo DPI |

Kiểm tra tương phản: trắng/Primary 8.32:1; chữ phụ/nền trắng 4.83:1; chữ trắng/xanh ngọc chỉ 3.32:1 nên không dùng cặp này cho chữ thường. Tất cả màu tập trung trong Theme.

## Control và trạng thái

- UiButton: primary/secondary/danger/ghost, focus bàn phím rõ, hover/pressed/disabled.
- UiCard, UiStatCard: bề mặt phân nhóm, số liệu có ý nghĩa và nhãn phạm vi.
- UiDataGridView: double buffering, header nhạt, dòng xen kẽ, không viền dọc; trạng thái có chữ và màu, preview chi tiết khi cần.
- UiSearchBox và FilterBar: nhãn rõ, Ctrl+F; bộ lọc tải lại catalog mới, giữ lựa chọn còn hợp lệ.
- UiEmptyState: giải thích chưa có dữ liệu hoặc không có kết quả tìm kiếm, gợi ý bước tiếp theo.
- UiToast: thành công/lỗi có chữ, không cản tác vụ thường; thông báo lỗi không tự mất trước khi đọc.
- UiConfirmDialog: nêu đúng đối tượng và hậu quả cho xóa/hủy/công bố/cấp quyền.
- Loading overlay: vô hiệu thao tác gây gửi trùng, async/await; dữ liệu cũ không bị biểu diễn thành số 0 khi tải lỗi.

Phím tắt: Ctrl+N thêm khi được phép, Ctrl+F tìm kiếm, F5 làm mới, Esc đóng dialog. AutoScaleMode.Dpi; dùng Dock/Anchor/TableLayoutPanel/FlowLayoutPanel.

## Đối chiếu màn hình và backend

| Màn | UC / FR | Nguồn dữ liệu và quyền |
|---|---|---|
| Đăng nhập | UC-01, A01–A04 | Auth API hiện có, chọn vai trò đã được cấp |
| Ngân hàng / chủ đề | UC-10, UC-22; C01–C10 | catalog, topics, questions; chỉ môn phân công |
| Soạn đề và điểm từng câu | UC-11; C11–C17 | exams và questionPoints; backend kiểm tra tổng 10 |
| Đợt / phân công | UC-18–20; D04–D06 | sessions, candidates; cán bộ phụ trách |
| Giám sát / kết quả | UC-12,13,21; D01–D03 | monitor, export, publish; đúng phạm vi |
| Người dùng / duyệt | UC-14–16; E01–E03 | users, approve, assignments; Administrator |

Dashboard chỉ tổng hợp thông tin được API hiện hành cho phép. Giảng viên xem số câu/chủ đề/đề và các kết quả trong môn được giao; cán bộ xem trạng thái đợt/phân công/kết quả; quản trị viên xem người dùng/đăng ký chờ duyệt/phân công. Không giả lập số đợt mà vai trò không được truy cập.

## Các điểm chưa có backend

Yêu cầu hình thức trong tệp đính kèm có nhắc Excel câu hỏi, sinh đề theo ma trận, lớp học, nhật ký hệ thống, cấu hình và phát hiện mất kết nối. Đây là các chức năng chưa được backend hiện tại cung cấp hoặc đã hoãn trong phạm vi trước. Không dựng nút hoạt động giả hay tự mở rộng quyền để lấp giao diện. Ghi rõ các điểm này trong bàn giao; đổi giao diện không được xem là đã hoàn thành nghiệp vụ mở rộng.

Giám sát có thể làm mới theo chu kỳ nhưng không được gọi là thời gian thực/mất kết nối nếu chỉ polling dữ liệu hiện có. Thời lượng và chính sách đợt vẫn do API kiểm tra. Việc kết thúc sớm đợt thi chưa nằm trong đợt thay đổi giao diện này.

## Kiểm chứng khi triển khai

Build Desktop riêng nếu cửa sổ người dùng giữ DLL. Dựng Form thật, dùng dữ liệu thật đã có và API theo từng vai trò; không tạo dữ liệu thử vào ngân hàng của người dùng cho mục đích chụp giao diện. Chụp đăng nhập, dashboard, ngân hàng câu hỏi, soạn đề, đợt thi và quản trị. Kiểm tra loading/rỗng/lỗi, phím tắt, DPI, bộ lọc mới và khả năng tiếp cận; ghi riêng kiểm tra offscreen và thao tác Windows tương tác.

Tham khảo: ui-ux-pro-max tìm kiếm education/examination/dashboard đưa ra Minimalism & Swiss phù hợp tính rõ ràng, nhưng phần hero/CTA và font web không phù hợp WinForms; dùng hệ màu/Segoe UI do người dùng chỉ định. frontend-design áp dụng hướng nhận diện giáo dục, không sao chép bố cục React.
