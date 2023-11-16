using System;
using System.IO;
using System.Linq;
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

    public static YamlMappingNode GetCurrentContext(YamlDocument yamlDocument)
    {
        var contexts = (YamlSequenceNode)yamlDocument.GetRootNode()["env"]["contexts"];

        if (!contexts.Any())
        {
            throw new ArgumentException(
                "The supplied ZAP Automation Framework YAML file should contain at least one context.");
        }

        var currentContext = (YamlMappingNode)contexts[0];

        if (contexts.Count() > 1)
        {
            currentContext = (YamlMappingNode)contexts.FirstOrDefault(context => context["Name"].ToString() == "Default Context")
                ?? currentContext;
        }

        return currentContext;
    }
}
