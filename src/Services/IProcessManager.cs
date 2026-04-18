using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bugay.Initialize.Docker.Services;

public interface IProcessManager : IAsyncDisposable
{
    void Start();
    void Kill();
    Task WaitForExitAsync(CancellationToken cancellationToken);
    int ExitCode { get; }
    int ProcessId { get; }
    string ProcessName { get; }
}
