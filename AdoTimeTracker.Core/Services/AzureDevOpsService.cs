using AdoTimeTracker.Core.Configuration;
using AdoTimeTracker.Core.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AdoTimeTracker.Core.Services;

public class AzureDevOpsService
{
    private readonly HttpClient _httpClient;
    private readonly string _project;
    private readonly string _team;
    private readonly string _organization;
    private readonly LogService _logService;

    public AzureDevOpsService(IOptions<AzureDevOpsSettings> options, LogService logService)
    {
        _logService = logService;
        var settings = options.Value;

        _organization = settings.Organization;
        _project = settings.Project;
        _team = settings.Team;

        _httpClient = new HttpClient();

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{settings.Pat}"));

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        _httpClient.BaseAddress = new Uri($"https://dev.azure.com/{settings.Organization}/");
    }

    public async Task<SprintInfo?> GetCurrentSprintAsync()
    {
        _logService.Info($"Getting current sprint for project '{_project}' and team '{_team}'");

        try
        {
            var url = $"{_project}/{_team}/_apis/work/teamsettings/iterations" +
                "?$timeframe=current&api-version=7.1";

            _logService.Info($"Calling Azure DevOps API: {url}");

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(json);

            var iterations = document.RootElement.GetProperty("value");

            if (iterations.GetArrayLength() == 0)
            {
                _logService.Info("No current sprint found");
                return null;
            }

            var sprint = iterations[0];

            var sprintInfo = new SprintInfo
            {
                Name = sprint.GetProperty("name").GetString() ?? "",

                Path = sprint.GetProperty("path").GetString() ?? "",

                StartDate = sprint.GetProperty("attributes").GetProperty("startDate").GetDateTime(),

                EndDate = sprint.GetProperty("attributes").GetProperty("finishDate").GetDateTime()
            };

            _logService.Info($"Successfully retrieved sprint: {sprintInfo.Name} ({sprintInfo.StartDate:yyyy-MM-dd} to {sprintInfo.EndDate:yyyy-MM-dd})");

            return sprintInfo;
        }
        catch (HttpRequestException ex)
        {
            _logService.Error($"HTTP error while getting current sprint: {ex.Message}");
            throw new InvalidOperationException($"Failed to retrieve current sprint from Azure DevOps: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logService.Error($"JSON parsing error while getting current sprint: {ex.Message}");
            throw new InvalidOperationException($"Failed to parse Azure DevOps response for current sprint: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error while getting current sprint: {ex.Message}");
            throw;
        }
    }

    public async Task<List<int>> GetMyTaskIdsAsync(string iterationPath)
    {
        if (string.IsNullOrWhiteSpace(iterationPath))
        {
            _logService.Error("Iteration path is null or empty");
            throw new ArgumentException("Iteration path cannot be null or empty", nameof(iterationPath));
        }

        _logService.Info($"Getting task IDs for iteration path: {iterationPath}");

        try
        {
            var wiql = new
            {
                query =
                $"""
                SELECT [System.Id]
                FROM WorkItems
                WHERE
                    [System.AssignedTo] = @Me
                    AND [System.WorkItemType] = 'Task'
                    AND [System.IterationPath] = '{iterationPath}'
                """
            };

            var content = new StringContent(JsonSerializer.Serialize(wiql), Encoding.UTF8, "application/json");

            _logService.Info("Executing WIQL query to get task IDs");

            var response = await _httpClient.PostAsync($"{_project}/_apis/wit/wiql?api-version=7.1", content);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(json);

            var taskIds = document.RootElement
                .GetProperty("workItems")
                .EnumerateArray()
                .Select(x => x.GetProperty("id").GetInt32())
                .ToList();

            _logService.Info($"Successfully retrieved {taskIds.Count} task ID(s)");

            return taskIds;
        }
        catch (HttpRequestException ex)
        {
            _logService.Error($"HTTP error while getting task IDs: {ex.Message}");
            throw new InvalidOperationException($"Failed to retrieve task IDs from Azure DevOps: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logService.Error($"JSON parsing error while getting task IDs: {ex.Message}");
            throw new InvalidOperationException($"Failed to parse Azure DevOps response for task IDs: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error while getting task IDs: {ex.Message}");
            throw;
        }
    }

    public async Task<List<WorkItemInfo>> GetWorkItemsAsync(IEnumerable<int> ids)
    {
        if (ids == null)
        {
            _logService.Error("Work item IDs collection is null");
            return [];
        }

        var idList = string.Join(",", ids);

        if (string.IsNullOrWhiteSpace(idList))
        {
            _logService.Info("No work item IDs provided, returning empty list");
            return [];
        }

        _logService.Info($"Getting work items for IDs: {idList}");

        try
        {
            var url =
                $"_apis/wit/workitems" +
                $"?ids={idList}" +
                $"&fields=System.Id," +
                $"System.Title," +
                $"System.State," +
                $"Microsoft.VSTS.Scheduling.CompletedWork," +
                $"Microsoft.VSTS.Scheduling.RemainingWork" +
                $"&api-version=7.1";

            _logService.Info($"Calling Azure DevOps API: {url}");

            var response = await _httpClient.GetAsync(url);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(json);

            var result = new List<WorkItemInfo>();

            foreach (var item in document.RootElement.GetProperty("value").EnumerateArray())
            {
                var fields = item.GetProperty("fields");

                var workItem = new WorkItemInfo
                {
                    Id = item.TryGetProperty("id", out var id)
                            ? id.GetInt32() : 0,

                    Title = fields.TryGetProperty("System.Title", out var title)
                            ? title.GetString() ?? ""
                            : "",

                    State = fields.TryGetProperty(
                            "System.State",
                            out var state)
                            ? state.GetString() ?? ""
                            : "",

                    CompletedWork = fields.TryGetProperty(
                            "Microsoft.VSTS.Scheduling.CompletedWork",
                            out var completed)
                            ? completed.GetDouble()
                            : 0,

                    RemainingWork = fields.TryGetProperty(
                            "Microsoft.VSTS.Scheduling.RemainingWork",
                            out var remaining)
                            ? remaining.GetDouble()
                            : 0,

                    Link = $"https://dev.azure.com/{_organization}/{_project}/_workitems/edit/{id}"
                };

                result.Add(workItem);
            }

            _logService.Info($"Successfully retrieved {result.Count} work item(s)");

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logService.Error($"HTTP error while getting work items: {ex.Message}");
            throw new InvalidOperationException($"Failed to retrieve work items from Azure DevOps: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            _logService.Error($"JSON parsing error while getting work items: {ex.Message}");
            throw new InvalidOperationException($"Failed to parse Azure DevOps response for work items: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error while getting work items: {ex.Message}");
            throw;
        }
    }
}