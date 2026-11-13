namespace Exam.Desktop;

// Each resource reuses the layout in ResourceView.Designer.cs.
public sealed class ExamPapersView : ResourceView
{
    public ExamPapersView() : base("exams") { }
    public ExamPapersView(ExamApiClient api) : base(api, "exams") { }
}