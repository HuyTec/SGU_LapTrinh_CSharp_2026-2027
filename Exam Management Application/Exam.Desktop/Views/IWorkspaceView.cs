namespace Exam.Desktop;

public interface IWorkspaceView
{
    string Title { get; }
    Task LoadAsync();
    Task RefreshAsync();
    void FocusSearch();
    Task AddNewAsync();
}
