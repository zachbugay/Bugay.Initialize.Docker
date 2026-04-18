using System;
using System.IO;
using Bugay.Initialize.Docker.Configuration;
using Bugay.Initialize.Docker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

try
{
    var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA")
        ?? throw new InvalidOperationException("LOCALAPPDATA environment variable is not set.");

    var logPath = Path.Combine(localAppData, "bugay-docker-installer", "logs", "log.txt");
    var template = "[{Timestamp:HH:mm:ss} {Level:u3}] ({ProcessName}/{ProcessId}) {Message:lj}{NewLine}{Exception}";

    Log.Logger = new LoggerConfiguration()
        .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, outputTemplate: template)
        .WriteTo.Console(outputTemplate: template)
        .Enrich.WithProcessId()
        .Enrich.WithProcessName()
        .CreateLogger();

    Log.Information("Starting Docker Initialization Service...");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((hostContext, services) =>
        {
            services.Configure<DockerWorkerOptions>(
                hostContext.Configuration.GetSection(DockerWorkerOptions.SectionName));
            services.AddSingleton<ProcessManager, ProcessManager>();
            services.AddHostedService<DockerWorker>();
        })
        .Build();

    await host.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}
