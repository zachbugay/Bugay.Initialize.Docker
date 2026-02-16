using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

try
{
    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    var logPath = Path.Combine(localAppData, "bugay-docker-installer", "logs", "log.txt");
    Log.Logger = new LoggerConfiguration()
    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] ({ProcessName}/{ProcessId}) {Message:lj}{NewLine}{Exception}")
    .Enrich.WithProcessId()
    .Enrich.WithProcessName()
    .CreateLogger();

    Log.Information("Starting Docker Initialization Service...");
    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureServices((hostContext, services) =>
        {
            services.AddHostedService<DockerWorker>();
        })
        .Build();
    await host.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}

public class DockerWorker : BackgroundService
{
    ILogger<DockerWorker> _logger;
    public DockerWorker(ILogger<DockerWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Begin Execute Async");
        var WslDistro = "Ubuntu";
        Process wslProcess = new Process();
        try
        {
            wslProcess.StartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = "wsl.exe",
                ArgumentList = { "-d", WslDistro, "--exec", "sh", "-c", "exec sleep infinity" },
                CreateNoWindow = true
            };

            wslProcess.OutputDataReceived += (sender, e) =>
            {
                if (e.Data is not null)
                    _logger.LogInformation("[wsl stdout] {Data}", e.Data);
            };
            wslProcess.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data is not null)
                    _logger.LogWarning("[wsl stderr] {Data}", e.Data);
            };

            wslProcess.Start();
            wslProcess.BeginOutputReadLine();
            wslProcess.BeginErrorReadLine();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError("Unable to start Ubuntu (Docker) process. Exception: {Exception}", ex);
            return;
        }

        _logger.LogInformation("Ubuntu (Docker) started. Process Name: {ProcessName}, PID: {PID}...", wslProcess.ProcessName, wslProcess.Id);

        await using var registration = stoppingToken.Register(() =>
        {
            try
            {
                if (!wslProcess.HasExited)
                {
                    _logger.LogInformation("Killing Process Name: {ProcessName}, PID: {PID}...", wslProcess.ProcessName, wslProcess.Id);
                    wslProcess.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("{Exception}", ex);
            }
        });

        try
        {
            await wslProcess.WaitForExitAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            await wslProcess.WaitForExitAsync();
        }
        _logger.LogInformation("WSL process exited with code {ExitCode}", wslProcess.ExitCode);
        _logger.LogInformation("End");
    }
}
