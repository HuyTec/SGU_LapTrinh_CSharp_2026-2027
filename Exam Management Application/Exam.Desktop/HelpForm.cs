using System.ComponentModel;

namespace Exam.Desktop;

public sealed partial class HelpForm : Form
{
    public HelpForm()
    {
        InitializeComponent();
        body.Text = Content(null);
    }

    public HelpForm(string? role) : this()
    {
        ApplyRole(role);
    }

    protected override void OnLoad(EventArgs e)
    {
        if (LicenseManager.UsageMode != LicenseUsageMode.Designtime && !DesignMode)
        {
            var area = Screen.FromControl(this).WorkingArea;
            MinimumSize = new Size(Math.Min(MinimumSize.Width, area.Width - 48), Math.Min(MinimumSize.Height, area.Height - 48));
            Size = new Size(Math.Min(Width, area.Width - 48), Math.Min(Height, area.Height - 48));
        }
        base.OnLoad(e);
    }

    private void ApplyRole(string? role)
    {
        title.Text = role is null ? "Bắt đầu sử dụng" : "Hướng dẫn — " + UiLabels.Of(role);
        body.Text = Content(role);
    }

    private static string Content(string? role)
    {
        var steps = role switch
        {
            "Administrator" => """
                QUẢN TRỊ TÀI KHOẢN

                1. Mở “Tài khoản / thí sinh”. Với tài khoản đã chờ duyệt từ trước, kiểm tra thông tin và cột “Vai trò đăng ký”, chọn đúng dòng rồi bấm “Duyệt đăng ký”.

                Bạn cũng có thể chọn Thêm để tạo trực tiếp tài khoản: điền tài khoản, họ tên, mật khẩu và chọn vai trò. Tài khoản thí sinh cần mã thí sinh.

                2. Muốn nhập nhiều thí sinh: chọn “Nhập CSV thí sinh”, lưu mẫu CSV, điền dữ liệu rồi chọn file UTF-8. Kiểm tra bản xem trước; máy chủ kiểm tra toàn bộ danh sách trước khi bạn xác nhận nhập. Tối đa 500 thí sinh / 1 MB.

                3. Mở “Phân công giảng viên”. Chọn tên giảng viên và môn học đã được cán bộ tạo. Muốn thay đổi, xóa phân công cũ rồi thêm lại.

                4. Chọn tài khoản để khóa/mở khóa hoặc “Đặt lại mật khẩu / vai trò”. Khi sửa, để trống mật khẩu sẽ giữ mật khẩu hiện tại. Thay đổi tài khoản có thể thu hồi các phiên đăng nhập của tài khoản đó.

                Một tài khoản có thể mang nhiều vai trò. Mỗi lần đăng nhập Desktop, người dùng chọn một vai trò đã được cấp cho phiên làm việc.
                """,
            "Lecturer" => """
                CHUẨN BỊ CÂU HỎI VÀ ĐỀ THI

                1. Kiểm tra môn học đã được quản trị viên phân công. Nếu chưa có môn, liên hệ quản trị viên.

                2. Mở “Chủ đề” → Thêm, chọn môn và đặt tên chủ đề.

                3. Mở “Ngân hàng câu hỏi”. Chọn chủ đề, nhập nội dung và bốn phương án khác nhau; chọn đúng một đáp án đúng. Có thể tìm kiếm và lọc theo môn, chủ đề, độ khó.

                4. Mở “Xây dựng đề thi”. Chọn môn, tên đề, đánh dấu các câu hỏi và nhập điểm từng câu. Tổng điểm cần bằng 10. Lưu đề rồi chọn “Xác nhận sẵn sàng”.

                5. Đề đã gán đợt thi hoặc câu hỏi đang được đề sẵn sàng sử dụng có thể bị khóa sửa. Đọc thông báo máy chủ; tạo câu hỏi mới khi cần.

                6. “Theo dõi / kết quả” hiển thị dữ liệu trong phạm vi môn được phân công. Chọn một dòng rồi xuất CSV/Excel cho đợt tương ứng.
                """,
            _ => """
                CHỌN ĐÚNG ỨNG DỤNG

                • Thí sinh và giảng viên có thể đăng ký trên Web tại /Account/Register. Bấm “Đăng ký tài khoản trên Web” ở màn đăng nhập để mở trang của máy chủ đang cấu hình.

                • Thí sinh đăng ký xong có thể đăng nhập Web để xem và làm bài thi được phân công.

                • Giảng viên đăng ký và đăng nhập trên Web. Quản trị viên cấp tài khoản quản trị trên Desktop và phân công môn cho giảng viên.

                ĐĂNG NHẬP DESKTOP

                1. Nhập địa chỉ máy chủ do nhóm vận hành cung cấp.

                2. Nhập tên đăng nhập, mật khẩu và chọn vai trò trong phiên làm việc. “Tự động chọn vai trò” dùng một vai trò nhân sự đã được cấp; lựa chọn này không cấp thêm quyền.

                3. Bấm Đăng nhập. Menu chỉ hiện chức năng phù hợp với vai trò hiện tại. Có thể mở “Hướng dẫn” trong ứng dụng để xem quy trình dành cho vai trò đó.

                4. Muốn đổi vai trò, đăng xuất rồi đăng nhập lại với vai trò đã được cấp. Nếu quên mật khẩu nhân sự, liên hệ quản trị viên.
                """
        };
        return steps.Trim() + "\n\nTHAO TÁC CHUNG\n\nChọn một dòng để xem/sửa hoặc thực hiện thao tác; bấm Làm mới sau khi dữ liệu thay đổi. Đọc thông báo lỗi trước khi thử lại. Nếu phiên bản dữ liệu đã thay đổi, đóng biểu mẫu và mở lại để tải bản mới.\n\nĐăng xuất khi kết thúc phiên làm việc. Thời hạn và quyền truy cập do máy chủ kiểm tra.";
    }
}
