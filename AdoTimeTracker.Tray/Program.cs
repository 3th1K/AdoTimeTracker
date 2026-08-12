using AdoTimeTracker.Core;
using AdoTimeTracker.Tray;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

ApplicationConfiguration.Initialize();

var builder =
    Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile(
    "appsettings.json",
    optional: false,
    reloadOnChange: true);

builder.Services
    .AddAdoTimeTrackerCore(builder.Configuration)
    .AddTray();

var host = builder.Build();

host.Services
    .GetRequiredService<TrayApplication>()
    .Run();