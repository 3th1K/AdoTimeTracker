using AdoTimeTracker.Core.Services;
using AdoTimeTracker.Forms;
using AdoTimeTracker.Tray;
using AdoTimeTracker.Tray.Forms;

public class TrayApplication
{
    private readonly ConfigService _configService;
    private readonly CancellationTokenSource _cts = new();
    private readonly NotificationService _notificationService;
    private readonly ReminderBackgroundService _reminderService;
    private readonly StartupService _startupService;
    private readonly SummaryService _summaryService;

    public TrayApplication(
        ReminderBackgroundService reminderService,
        SummaryService summaryService,
        ConfigService configService,
        StartupService startupService,
        NotificationService notificationService)
    {
        _reminderService = reminderService;
        _summaryService = summaryService;
        _configService = configService;
        _startupService = startupService;
        _notificationService = notificationService;
    }

    public void Run()
    {
        var trayContext =
            new TrayApplicationContext();

        trayContext.StartupMenuItem.Checked =
            _startupService.IsEnabled();

        _notificationService.Initialize(
            trayContext.NotifyIcon);

        RegisterEvents(trayContext);

        Task.Run(() =>
            _reminderService.StartAsync(
                _cts.Token));

        Application.Run(trayContext);

        _cts.Cancel();
    }

    private void RegisterEvents(
        TrayApplicationContext trayContext)
    {
        trayContext.CheckRequested += async () =>
        {
            await _reminderService.CheckNowAsync();
        };

        trayContext.StatusRequested += async () =>
        {
            var summary =
                await _summaryService.GetSummaryAsync();

            if (summary is null)
                return;

            using var form =
                new StatusForm(summary);

            form.ShowDialog();
        };

        trayContext.SettingsRequested += () =>
        {
            using var form =
                new SettingsForm(_configService);

            form.ShowDialog();
        };

        trayContext.StartupToggleRequested += () =>
        {
            if (_startupService.IsEnabled())
            {
                _startupService.Disable();
            }
            else
            {
                _startupService.Enable();
            }

            trayContext.StartupMenuItem.Checked =
                _startupService.IsEnabled();
        };
    }
}