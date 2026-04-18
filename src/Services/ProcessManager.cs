using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bugay.Initialize.Docker.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bugay.Initialize.Docker.Services;

public class ProcessManager : IProcessManager
{
    private readonly Process _process;
    private readonly ILogger<ProcessManager> _logger;

    public int ExitCode => _process.ExitCode;
    public int ProcessId => _process.Id;
    public string ProcessName => _process.ProcessName;

    public ProcessManager(IOptions<DockerWorkerOptions> options, ILogger<ProcessManager> logger)
    {
        _logger = logger;

        var config = options.Value;
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = "wsl.exe",
                ArgumentList = { "-d", config.WslDistro, "--exec", "sh", "-c", config.WslCommand },
                CreateNoWindow = true
            }
        };

        _process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
                _logger.LogInformation("[wsl stdout] {Data}", e.Data);
        };

        _process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data is not null)
                _logger.LogWarning("[wsl stderr] {Data}", e.Data);
        };
    }

    public void Start()
    {
        try
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
            _logger.LogError("Unable to start WSL process. Exception: {Exception}", ex);
            throw;
        }
    }

    public void Kill()
    {
        try
        {
            _process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Process already exited: {Exception}", ex);
        }
    }

    public Task WaitForExitAsync(CancellationToken cancellationToken)
    {
        return _process.WaitForExitAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        _process.Dispose();
        return ValueTask.CompletedTask;
    }
}
