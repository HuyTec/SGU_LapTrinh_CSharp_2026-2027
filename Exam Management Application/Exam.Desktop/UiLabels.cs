namespace Exam.Desktop;

internal static class UiLabels
{
    private static readonly Dictionary<string, string> Labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Administrator"] = "Quản trị viên", ["Lecturer"] = "Giảng viên", ["ExamOfficer"] = "Cán bộ quản lý thi", ["Student"] = "Thí sinh",
        ["Draft"] = "Nháp", ["Ready"] = "Sẵn sàng", ["Active"] = "Đã kích hoạt", ["Open"] = "Đang mở", ["Closed"] = "Đã đóng", ["Cancelled"] = "Đã hủy",
        ["NotStarted"] = "Chưa thi", ["InProgress"] = "Đang làm", ["Submitted"] = "Đã nộp", ["Finished"] = "Đã kết thúc", ["Completed"] = "Đã kết thúc",
        ["Easy"] = "Dễ", ["Medium"] = "Trung bình", ["Hard"] = "Khó",
        ["ScoreOnly"] = "Chỉ xem điểm", ["Answers"] = "Xem điểm và bài đã làm", ["CorrectAnswers"] = "Xem điểm, bài làm và đáp án đúng",
        ["True"] = "Có", ["False"] = "Không"
    };
    public static string Of(string value) => Labels.GetValueOrDefault(value, value);
}
