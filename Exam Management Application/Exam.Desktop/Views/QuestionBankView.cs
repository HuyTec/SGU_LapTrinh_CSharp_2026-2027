namespace Exam.Desktop;

// Each resource reuses the layout in ResourceView.Designer.cs.
public sealed class QuestionBankView : ResourceView
{
    public QuestionBankView() : base("questions") { }
    public QuestionBankView(ExamApiClient api) : base(api, "questions") { }
}