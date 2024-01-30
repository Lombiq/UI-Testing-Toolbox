using System.IO;
using YamlDotNet.RepresentationModel;

namespace Lombiq.Tests.UI.SecurityScanning;

public static class YamlHelper
{
    public static YamlDocument LoadDocument(string yamlFilePath)
    {
        using var streamReader = new StreamReader(yamlFilePath);
        var yamlStream = new YamlStream();
        yamlStream.Load(streamReader);
        return yamlStream.Documents[0];
    }
}
