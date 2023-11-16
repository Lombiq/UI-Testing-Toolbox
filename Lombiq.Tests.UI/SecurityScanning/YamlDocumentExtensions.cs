using System;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Lombiq.Tests.UI.SecurityScanning;

public static class YamlDocumentExtensions
{
    /// <summary>
    /// Adds the <see href="https://www.zaproxy.org/docs/desktop/addons/ajax-spider/automation/">ZAP Ajax Spider</see>
    /// to the ZAP Automation Framework plan, just after the "spider" job.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// If no job named "spider" is found in the ZAP Automation Framework plan.
    /// </exception>
    public static YamlDocument AddSpiderAjaxAfterSpider(this YamlDocument yamlDocument)
    {
        var jobs = (YamlSequenceNode)yamlDocument.GetRootNode()["jobs"];

        var spiderJob =
            jobs.FirstOrDefault(job => (string)job["name"] == "spider") ??
            throw new ArgumentException(
                "No job named \"spider\" found in the Automation Framework Plan. We can only add ajaxSpider immediately after it.");

        var spiderIndex = jobs.Children.IndexOf(spiderJob);
        var spiderAjax = YamlHelper.LoadDocument(AutomationFrameworkPlanFragmentsPaths.SpiderAjaxPath);
        jobs.Children.Insert(spiderIndex + 1, spiderAjax.GetRootNode());

        return yamlDocument;
    }

    /// <summary>
    /// Adds one or more regex patterns to the ZAP Automation Framework plan's excludePaths config under the current
    /// context.
    /// </summary>
    public static YamlDocument AddExcludePathsRegex(this YamlDocument yamlDocument, params string[] excludePathsPatterns)
    {
        var currentContext = YamlHelper.GetCurrentContext(yamlDocument);

        if (!currentContext.Children.ContainsKey("excludePaths")) currentContext.Add("excludePaths", new YamlSequenceNode());

        var excludePaths = (YamlSequenceNode)currentContext["excludePaths"];
        foreach (var pattern in excludePathsPatterns)
        {
            excludePaths.Add(pattern);
        }

        return yamlDocument;
    }

    public static YamlMappingNode GetRootNode(this YamlDocument yamlDocument) => (YamlMappingNode)yamlDocument.RootNode;
}
