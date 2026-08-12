using AdoTimeTracker.Core.Configuration;
using Microsoft.Extensions.Options;

namespace AdoTimeTracker.Core.Services;

public class ReminderBackgroundService
{
    private readonly LogService _logService;
    private readonly NotificationService _notificationService;
    private readonly IOptionsMonitor<ReminderSettings> _settings;
    private readonly SummaryService _summaryService;

    public ReminderBackgroundService(
        SummaryService summaryService,
        NotificationService notificationService,
        LogService logService,
        IOptionsMonitor<ReminderSettings> settings)
    {
        _summaryService = summaryService ?? throw new ArgumentNullException(nameof(summaryService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logService = logService ?? throw new ArgumentNullException(nameof(logService));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _logService.Info("ReminderBackgroundService initialized");
    }

    public async Task CheckNowAsync()
    {
        _logService.Info("Manual check triggered");

        try
        {
            _logService.Info("Fetching time tracking summary");

            var summary =
                await _summaryService.GetSummaryAsync();

            if (summary is null)
            {
                _logService.Info("No summary available (no current sprint found)");
                return;
            }

            _logService.Info($"Summary retrieved - Expected: {summary.ExpectedHours}, Logged: {summary.LoggedHours}, Pending: {summary.PendingHours}");

            if (summary.PendingHours > 0)
            {
                _logService.Info($"Showing reminder notification for {summary.PendingHours} pending hour(s)");
                _notificationService.ShowReminder(
                    summary.PendingHours);
            }
            else if (summary.PendingHours <= 0)
            {
                _logService.Info("All hours logged, showing success notification");
                _notificationService.ShowSuccess();
            }

            _logService.Info("Manual check completed successfully");
        }
        catch (InvalidOperationException ex)
        {
            _logService.Error($"Operation error during manual check: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error during manual check: {ex.Message}");
            throw new InvalidOperationException($"Failed to perform manual check: {ex.Message}", ex);
        }
    }

    public async Task StartAsync(
            CancellationToken cancellationToken)
    {
        _logService.Info("ReminderBackgroundService started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var settings =
                    _settings.CurrentValue;

                if (settings.IntervalMinutes <= 0)
                {
                    _logService.Error($"Invalid interval configured: {settings.IntervalMinutes} minutes");
                    throw new InvalidOperationException($"Interval must be greater than zero. Current value: {settings.IntervalMinutes}");
                }

                _logService.Info(
                    $"Checking Azure DevOps. StartHour={settings.StartHour}, EndHour={settings.EndHour}, IntervalMinutes={settings.IntervalMinutes}");

                var hour = DateTime.Now.Hour;

                if (hour >= settings.StartHour &&
                    hour <= settings.EndHour)
                {
                    _logService.Info($"Current hour ({hour}) is within reminder window, fetching summary");

                    var summary =
                        await _summaryService.GetSummaryAsync();

                    if (summary is null)
                    {
                        _logService.Info("No summary available (no current sprint found)");
                    }
                    else
                    {
                        _logService.Info(
                            $"Expected={summary.ExpectedHours}, Logged={summary.LoggedHours}, Pending={summary.PendingHours}");

                        if (summary.PendingHours > 0)
                        {
                            _logService.Info($"Showing reminder notification for {summary.PendingHours} pending hour(s)");
                            _notificationService.ShowReminder(
                                summary.PendingHours);
                        }
                        else
                        {
                            _logService.Info("All hours logged for the period");
                        }
                    }
                }
                else
                {
                    _logService.Info($"Current hour ({hour}) is outside reminder window ({settings.StartHour}-{settings.EndHour}), skipping check");
                }

                var nextRun =
    GetNextRunTime(
        DateTime.Now,
        settings.IntervalMinutes);

                var delay =
                    nextRun - DateTime.Now;

                _logService.Info($"Next check scheduled at {nextRun:yyyy-MM-dd HH:mm:ss} (delay: {delay.TotalMinutes:F1} minutes)");

                await Task.Delay(
                    delay,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logService.Info("ReminderBackgroundService cancellation requested");
                break;
            }
            catch (InvalidOperationException ex)
            {
                _logService.Error($"Operation error in background service: {ex.Message}");

                _logService.Info("Waiting 5 minutes before retry");

                await Task.Delay(
                    TimeSpan.FromMinutes(5),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logService.Error(
                    $"Unexpected error in background service: {ex.Message}\n{ex.StackTrace}");

                _logService.Info("Waiting 5 minutes before retry");

                await Task.Delay(
                    TimeSpan.FromMinutes(5),
                    cancellationToken);
            }
        }

        _logService.Info("ReminderBackgroundService stopped");
    }

    private DateTime GetNextRunTime(
    DateTime now,
    int intervalMinutes)
    {
        if (intervalMinutes <= 0)
        {
            _logService.Error($"Invalid interval minutes: {intervalMinutes}");
            throw new ArgumentException("Interval minutes must be greater than zero", nameof(intervalMinutes));
        }

        var remainder =
            now.Minute % intervalMinutes;

        var minutesToAdd =
            intervalMinutes - remainder;

        if (minutesToAdd == intervalMinutes)
        {
            minutesToAdd = 0;
        }

        var nextRun = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            0,
            0)
            .AddMinutes(now.Minute + minutesToAdd);

        return nextRun;
    }
}