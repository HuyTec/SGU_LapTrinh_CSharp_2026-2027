namespace Exam.Desktop;

// Each resource reuses the layout in ResourceView.Designer.cs.
public sealed class ExamSessionsView : ResourceView
{
    public ExamSessionsView() : base("sessions") { }
    public ExamSessionsView(ExamApiClient api) : base(api, "sessions") { }
}