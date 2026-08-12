using Microsoft.Extensions.DependencyInjection;

namespace AdoTimeTracker.Tray;

public static class DependencyInjection
{
    public static IServiceCollection AddTray(
        this IServiceCollection services)
    {
        services.AddSingleton<TrayApplication>();

        return services;
    }
}