namespace Bugay.Initialize.Docker.Configuration;

public class DockerWorkerOptions
{
    public const string SectionName = "DockerWorker";
    public string WslDistro { get; set; } = "Ubuntu";
    public string WslCommand { get; set; } = "exec sleep infinity";
}
