using AdoTimeTracker.Core.Configuration;
using AdoTimeTracker.Core.Models;
using Microsoft.Extensions.Options;

namespace AdoTimeTracker.Core.Services;

public class SummaryService
{
    private readonly AzureDevOpsService _adoService;
    private readonly LeaveService _leaveService;
    private readonly IOptionsMonitor<WorkHoursSettings> _workHours;
    private readonly LogService _logService;

    public SummaryService(
        AzureDevOpsService adoService,
        LeaveService leaveService,
        IOptionsMonitor<WorkHoursSettings> workHours, LogService logService)
    {
        _adoService = adoService;
        _leaveService = leaveService;
        _workHours = workHours;
        _logService = logService;
    }

    public async Task<TimeTrackingSummary?> GetSummaryAsync()
    {
        _logService.Info("Starting to generate time tracking summary");

        try
        {
            _logService.Info("Fetching current sprint information");
            var sprint =
                await _adoService.GetCurrentSprintAsync();

            if (sprint is null)
            {
                _logService.Info("No current sprint found, returning null");
                return null;
            }

            _logService.Info($"Sprint found: {sprint.Name}");

            _logService.Info("Fetching task IDs for current sprint");
            var ids =
                await _adoService.GetMyTaskIdsAsync(
                    sprint.Path);

            _logService.Info($"Found {ids.Count} task ID(s)");

            _logService.Info("Fetching work item details");
            var tasks =
                await _adoService.GetWorkItemsAsync(ids);

            _logService.Info($"Retrieved {tasks.Count} work item(s)");

            _logService.Info("Fetching leave days");
            var leaveDays =
                _leaveService.GetLeaves();

            _logService.Info($"Found {leaveDays.Count} leave day(s)");

            _logService.Info("Calculating working days elapsed");
            var workingDays =
                SprintCalculator.GetWorkingDaysElapsed(
                    sprint.StartDate,
                    DateTime.Today,
                    leaveDays);

            _logService.Info($"Working days elapsed: {workingDays}");

            var dailyHours =
                _workHours.CurrentValue.DailyHours;

            if (dailyHours <= 0)
            {
                _logService.Error($"Invalid daily hours configured: {dailyHours}");
                throw new InvalidOperationException($"Daily hours must be greater than zero. Current value: {dailyHours}");
            }

            _logService.Info($"Daily hours configured: {dailyHours}");

            var expectedHours =
                workingDays * dailyHours;

            var loggedHours =
                tasks.Sum(x => x.CompletedWork);

            var pendingHours =
                expectedHours - loggedHours;

            _logService.Info($"Expected hours: {expectedHours}, Logged hours: {loggedHours}, Pending hours: {pendingHours}");

            var appliedLeaves =
        leaveDays.Count(x =>
            x.Date >= sprint.StartDate.Date &&
            x.Date <= sprint.EndDate.Date);

            _logService.Info($"Leave days applied in current sprint: {appliedLeaves}");

            var workItemsSummaries = tasks
                .GroupBy(t => t.State)
                .Select(d => new WorkItemsSummary
                {
                    State = d.Key,
                    Count = d.Count(),
                    WorkItems = d.ToList()
                }).ToList();

            _logService.Info($"Work items grouped into {workItemsSummaries.Count} state(s)");

            var summary = new TimeTrackingSummary
            {
                SprintName = sprint.Name,
                WorkingDaysElapsed = workingDays,
                LeaveDaysApplied = appliedLeaves,
                ExpectedHours = expectedHours,
                LoggedHours = loggedHours,
                PendingHours = pendingHours,
                WorkItemsSummaries = workItemsSummaries
            };

            _logService.Info("Successfully generated time tracking summary");

            return summary;
        }
        catch (InvalidOperationException ex)
        {
            _logService.Error($"Operation error while generating summary: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logService.Error($"Unexpected error while generating summary: {ex.Message}");
            throw new InvalidOperationException($"Failed to generate time tracking summary: {ex.Message}", ex);
        }
    }
}