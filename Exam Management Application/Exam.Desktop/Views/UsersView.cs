namespace Exam.Desktop;

// Each resource reuses the layout in ResourceView.Designer.cs.
public sealed class UsersView : ResourceView
{
    public UsersView() : base("users") { }
    public UsersView(ExamApiClient api) : base(api, "users") { }
}