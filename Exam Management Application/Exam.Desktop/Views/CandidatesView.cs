namespace Exam.Desktop;

// Each resource reuses the layout in ResourceView.Designer.cs.
public sealed class CandidatesView : ResourceView
{
    public CandidatesView() : base("candidates") { }
    public CandidatesView(ExamApiClient api) : base(api, "candidates") { }
}