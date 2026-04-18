using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bugay.Initialize.Docker.Services;

public class DockerWorker : BackgroundService
{
    private readonly ProcessManager _processManager;
    private readonly ILogger<DockerWorker> _logger;

    public DockerWorker(ProcessManager processManager, ILogger<DockerWorker> logger)
    {
        _processManager = processManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Begin Execute Async");

        try
        {
            _processManager.Start();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            _logger.LogError("Unable to start Ubuntu (Docker) process. Exception: {Exception}", ex);
            return;
        }

        _logger.LogInformation(
            "Ubuntu (Docker) started. Process Name: {ProcessName}, PID: {PID}...",
            _processManager.ProcessName,
            _processManager.ProcessId);

        await using var registration = stoppingToken.Register(() =>
        {
            _logger.LogInformation(
                "Killing Process Name: {ProcessName}, PID: {PID}...",
                _processManager.ProcessName,
                _processManager.ProcessId);
            _processManager.Kill();
        });

        try
        {
            await _processManager.WaitForExitAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            await _processManager.WaitForExitAsync(CancellationToken.None);
        }

        _logger.LogInformation("WSL process exited with code {ExitCode}", _processManager.ExitCode);
        _logger.LogInformation("End");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await _processManager.DisposeAsync();
    }
}
