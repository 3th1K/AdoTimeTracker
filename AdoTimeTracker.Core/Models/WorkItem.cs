namespace AdoTimeTracker.Core.Models;

public class WorkItemInfo
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Link { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public double CompletedWork { get; set; }

    public double RemainingWork { get; set; }
}