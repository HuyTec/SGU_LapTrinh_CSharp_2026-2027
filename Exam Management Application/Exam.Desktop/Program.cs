namespace Exam.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.SetDefaultFont(new Font("Arial", 10));
        using var api = new ExamApiClient();
        Application.Run(new LoginForm(api));
    }
}
