namespace AdoTimeTracker.Core.Services;

public static class SprintCalculator
{
    public static int GetWorkingDaysElapsed(
        DateTime sprintStart,
        DateTime currentDate,
        IEnumerable<DateTime> leaveDays)
    {
        int count = 0;

        var leaveSet =
            leaveDays
                .Select(x => x.Date)
                .ToHashSet();

        for (
            var date = sprintStart.Date;
            date <= currentDate.Date;
            date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday
                or DayOfWeek.Sunday)
            {
                continue;
            }

            if (leaveSet.Contains(date))
            {
                continue;
            }

            count++;
        }

        return count;
    }
}