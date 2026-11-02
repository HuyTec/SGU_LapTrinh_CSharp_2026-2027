namespace Exam.Desktop;

/// <summary>Owns one active workspace at a time, including its polling timers and controls.</summary>
public sealed class NavigationService(Panel host, Func<string, UserControl> factory) : IDisposable
{
    public UserControl? Current { get; private set; }
    public string CurrentKey { get; private set; } = "";

    public async Task NavigateAsync(string key)
    {
        var next = factory(key);
        var previous = Current;
        host.SuspendLayout();
        host.Controls.Clear();
        Current = next;
        CurrentKey = key;
        next.Dock = DockStyle.Fill;
        host.Controls.Add(next);
        host.ResumeLayout(true);
        previous?.Dispose();
        if (next is IWorkspaceView workspace) await workspace.LoadAsync();
    }

    public void Dispose() => Current?.Dispose();
}
