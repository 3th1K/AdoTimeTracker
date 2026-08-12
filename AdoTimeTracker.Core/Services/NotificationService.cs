namespace AdoTimeTracker.Core.Services;

public class NotificationService
{
    private NotifyIcon? _notifyIcon;

    public void Initialize(NotifyIcon notifyIcon)
    {
        _notifyIcon = notifyIcon;
    }

    public void ShowReminder(double pendingHours)
    {
        _notifyIcon?.ShowBalloonTip(
            10000,
            "Azure DevOps Reminder",
            $"You still need to log {pendingHours} hour(s).",
            ToolTipIcon.Warning);
    }

    public void ShowSuccess()
    {
        _notifyIcon?.ShowBalloonTip(
            5000,
            "Azure DevOps Reminder",
            "All required hours have been logged. Great job!",
            ToolTipIcon.Info);
    }
}