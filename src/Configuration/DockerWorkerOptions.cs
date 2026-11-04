namespace Bugay.Initialize.Docker.Configuration;

public class DockerWorkerOptions
{
    public const string SectionName = "DockerWorker";
    public string WslDistro { get; set; } = "Ubuntu-26.04 (Default)";
    public string WslCommand { get; set; } = "exec sleep infinity";
}
