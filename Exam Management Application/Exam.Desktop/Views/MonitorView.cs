namespace Exam.Desktop;

// Each resource reuses the layout in ResourceView.Designer.cs.
public sealed class MonitorView : ResourceView
{
    public MonitorView() : base("monitor") { }
    public MonitorView(ExamApiClient api) : base(api, "monitor") { }
}