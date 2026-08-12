namespace AdoTimeTracker.Core.Models;

public class WorkItemsSummary
{
    public string State { get; set; } = string.Empty;
    public int Count { get; set; }
    public List<WorkItemInfo> WorkItems { get; set; } = [];
}