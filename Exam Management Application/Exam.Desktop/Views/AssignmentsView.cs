namespace Exam.Desktop;

// Each resource reuses the layout in ResourceView.Designer.cs.
public sealed class AssignmentsView : ResourceView
{
    public AssignmentsView() : base("assignments") { }
    public AssignmentsView(ExamApiClient api) : base(api, "assignments") { }
}