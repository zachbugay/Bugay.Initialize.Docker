using System;
using System.IO;
using Bugay.Initialize.Docker.Configuration;
using Bugay.Initialize.Docker.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

try
{
    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    var logDirectory = Path.Combine(localAppData, "bugay-docker-installer", "logs");

    var logFileName = $"bugay.initialze.docker-{DateTime.Now:yyyy-MM-dd}.log";
    var template = "[{Timestamp:HH:mm:ss} {Level:u3}] ({ProcessName}/{ProcessId}) {Message:lj}{NewLine}{Exception}";

    Log.Logger = new LoggerConfiguration()
        .WriteTo.File(Path.Combine(logDirectory, logFileName),
            rollingInterval: RollingInterval.Infinite,
            retainedFileCountLimit: 5,
            outputTemplate: template)
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
