namespace AdoTimeTracker.Core.Models;

public class SettingsViewModel
{
    public int DailyHours { get; set; }

    public int StartHour { get; set; }

    public int EndHour { get; set; }

    public int IntervalMinutes { get; set; }

    public List<DateTime> LeaveDays { get; set; } = [];
}