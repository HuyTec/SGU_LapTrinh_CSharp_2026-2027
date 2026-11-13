namespace Exam.Desktop;

// Each resource reuses the layout in ResourceView.Designer.cs.
public sealed class TopicsView : ResourceView
{
    public TopicsView() : base("topics") { }
    public TopicsView(ExamApiClient api) : base(api, "topics") { }
}