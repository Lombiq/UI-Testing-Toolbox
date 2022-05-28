using Newtonsoft.Json;

namespace Lombiq.Tests.UI.Models;

public class DockerConfiguration
{
    public string ContainerName { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string ContainerSnapshotPath { get; set; } = "/data/Snapshots";
}
