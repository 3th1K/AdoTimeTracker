namespace AdoTimeTracker.Core.Models;

public class TimeTrackingSummary
{
    public string SprintName { get; set; } = string.Empty;

    public int WorkingDaysElapsed { get; set; }

    public int LeaveDaysApplied { get; set; }

    public double ExpectedHours { get; set; }

    public double LoggedHours { get; set; }

    public double PendingHours { get; set; }

    public List<WorkItemsSummary> WorkItemsSummaries { get; set; } = [];
}