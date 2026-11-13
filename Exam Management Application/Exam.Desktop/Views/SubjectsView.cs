namespace Exam.Desktop;

// Each resource reuses the layout in ResourceView.Designer.cs.
public sealed class SubjectsView : ResourceView
{
    public SubjectsView() : base("subjects") { }
    public SubjectsView(ExamApiClient api) : base(api, "subjects") { }
}