namespace AdoTimeTracker.Core.Configuration;

public class AzureDevOpsSettings
{
    public string Organization { get; set; } = string.Empty;

    public string Project { get; set; } = string.Empty;

    public string Team { get; set; } = string.Empty;

    public string Pat { get; set; } = string.Empty;
}