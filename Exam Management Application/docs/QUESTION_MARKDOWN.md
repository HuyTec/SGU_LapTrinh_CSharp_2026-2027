# Định dạng câu hỏi trên Web

Giảng viên vào Ngân hàng câu hỏi → Thêm/Sửa. Nội dung và cả bốn đáp án
chấp nhận Markdown, có xem trước khi nhập. Dữ liệu lưu nguyên văn; không đổi schema.

```text
**Tính tích phân** của hàm số $f(x)=x^2$.

$$\int_0^1 x^2\,dx = \frac{1}{3}$$
```

Hỗ trợ in đậm, nghiêng, danh sách, bảng, mã nguồn và công thức LaTeX
`$...$`, `$$...$$`, `\(...\)`, `\[...\]`.
Ngân hàng, màn hình chọn câu cho đề, bài thi và xem lại kết quả dùng cùng
`question-content.js`. Câu đúng màu xanh, sai/chưa trả lời màu đỏ sau khi công bố
chế độ xem đáp án; chế độ chỉ điểm không gửi nội dung xem lại.

Không cho HTML thô hoặc ảnh nhúng; liên kết nguy hiểm bị markdown-it chặn.
KaTeX cấu hình `trust:false`, giới hạn kích thước và mở rộng macro.
Thư viện/phông được phục vụ cục bộ, không tải CDN khi thi.
Công thức lỗi hiện lại nguồn thay vì ngăn tải bài thi. Khi không có JavaScript,
nội dung hiện ở dạng nguồn (làm bài vốn yêu cầu JavaScript).
WinForms hiện hiển thị nguồn văn bản, chưa có trình hiển thị công thức.

Nguồn thư viện MIT, giấy phép đi kèm dưới `wwwroot/lib`:

- [markdown-it 14.1.0](https://github.com/markdown-it/markdown-it/tree/14.1.0)
- [markdown-it-texmath 1.0.0](https://github.com/goessner/markdown-it-texmath)
- [KaTeX 0.19.1](https://github.com/KaTeX/KaTeX), [hướng dẫn bảo mật](https://katex.org/docs/security)

Kiểm tra bộ hiển thị: `node scripts/Test-QuestionContent.cjs`.
