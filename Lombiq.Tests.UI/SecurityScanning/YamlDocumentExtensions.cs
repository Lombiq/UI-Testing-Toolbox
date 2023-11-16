using System;
using System.Linq;
using System.Threading.Tasks;
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
        var jobs = yamlDocument.GetJobs();

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

    /// <summary>
    /// Disable a certain ZAP scan rule.
    /// </summary>
    /// <param name="id">The ID of the rule. In the scan report, this is usually displayed as "Plugin Id".</param>
    /// <param name="name">
    /// The human-readable name of the rule. Not required to turn off the rule, and its value doesn't matter. It's just
    /// useful for the readability of the method call.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if no job with the type "passiveScan-config" is found in the Automation Framework Plan.
    /// </exception>
    public static YamlDocument DisableScanRule(this YamlDocument yamlDocument, int id, string name = "")
    {
        var jobs = yamlDocument.GetJobs();

        var passiveScanConfigJob =
            (YamlMappingNode)jobs.FirstOrDefault(job => (string)job["type"] == "passiveScan-config") ??
            throw new ArgumentException(
                "No job with the type \"passiveScan-config\" found in the Automation Framework Plan so the rule can't be added.");

        if (!passiveScanConfigJob.Children.ContainsKey("rules")) passiveScanConfigJob.Add("rules", new YamlSequenceNode());

        var newRule = new YamlMappingNode
        {
            { "id", id.ToTechnicalString() },
            { "name", name },
            { "threshold", "off" },
        };

        ((YamlSequenceNode)passiveScanConfigJob["rules"]).Add(newRule);

        return yamlDocument;
    }

    /// <summary>
    /// Gets <see cref="YamlDocument.RootNode"/> cast to <see cref="YamlMappingNode"/>.
    /// </summary>
    public static YamlMappingNode GetRootNode(this YamlDocument yamlDocument) => (YamlMappingNode)yamlDocument.RootNode;

    /// <summary>
    /// Gets the "jobs" section of the <see cref="YamlDocument"/>.
    /// </summary>
    public static YamlSequenceNode GetJobs(this YamlDocument yamlDocument) =>
        (YamlSequenceNode)yamlDocument.GetRootNode()["jobs"];

    /// <summary>
    /// Shortcuts to <see cref="Task.CompletedTask"/> to be able to chain <see cref="YamlDocument"/> extensions in an
    /// async method/delegate.
    /// </summary>
    /// <returns><see cref="Task.CompletedTask"/>.</returns>
    public static Task CompletedTaskAsync(this YamlDocument yamlDocument) => Task.CompletedTask;
}
