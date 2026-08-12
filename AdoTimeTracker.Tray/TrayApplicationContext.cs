namespace AdoTimeTracker.Tray;

public class TrayApplicationContext : ApplicationContext
{
    public NotifyIcon NotifyIcon { get; }

    public event Action? CheckRequested;

    public event Func<Task>? StatusRequested;

    public event Action? SettingsRequested;

    public ToolStripMenuItem StartupMenuItem { get; private set; } = null!;

    public event Action? StartupToggleRequested;

    public TrayApplicationContext()
    {
        var ico = new Icon(
        Path.Combine(
            AppContext.BaseDirectory,
            "Resources",
            "app.ico"));
        NotifyIcon = new NotifyIcon
        {
            // BalloonTipIcon = ico.ToBitmap() ,

            Icon = ico,

            Visible = true,
            Text = "Azure DevOps Tracker"
        };

        var menu = new ContextMenuStrip();

        menu.Items.Add(
    "Check Now",
    null,
    (_, _) => CheckRequested?.Invoke());

        StartupMenuItem = new ToolStripMenuItem("Run on Startup");

        StartupMenuItem.Click += (_, _) =>
        {
            StartupToggleRequested?.Invoke();
        };

        menu.Items.Add(StartupMenuItem);

        menu.Items.Add(
            "Status",
            null,
            async (_, _) =>
            {
                if (StatusRequested != null)
                {
                    await StatusRequested.Invoke();
                }
            });
        menu.Items.Add(
    "Settings",
    null,
    (_, _) =>
    {
        SettingsRequested?.Invoke();
    });

        menu.Items.Add(
            "Exit",
            null,
            (_, _) => ExitThread());
        NotifyIcon.ContextMenuStrip = menu;
    }

    protected override void Dispose(bool disposing)
    {
        NotifyIcon.Visible = false;
        NotifyIcon.Dispose();

        base.Dispose(disposing);
    }
}