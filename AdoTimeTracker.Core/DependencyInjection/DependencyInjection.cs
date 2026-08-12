using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AdoTimeTracker.Core.Configuration;
using AdoTimeTracker.Core.Services;

namespace AdoTimeTracker.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddAdoTimeTrackerCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AzureDevOpsSettings>(configuration.GetSection("AzureDevOps"));

        services.Configure<WorkHoursSettings>(configuration.GetSection("WorkHours"));

        services.Configure<ReminderSettings>(configuration.GetSection("Reminder"));

        services.AddSingleton<LogService>();

        services.AddSingleton<AzureDevOpsService>();

        services.AddSingleton<LeaveService>();

        services.AddSingleton<SummaryService>();

        services.AddSingleton<NotificationService>();

        services.AddSingleton<ReminderBackgroundService>();

        services.AddSingleton<StartupService>();

        services.AddSingleton<ConfigService>();

        return services;
    }
}