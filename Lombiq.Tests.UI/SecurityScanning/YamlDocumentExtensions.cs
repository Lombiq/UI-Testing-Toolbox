using System;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace Lombiq.Tests.UI.SecurityScanning;

public static class YamlDocumentExtensions
{
    /// <summary>
    /// Sets the start URL under the app where to start the scan from in the current context of the ZAP Automation
    /// Framework plan.
    /// </summary>
    /// <param name="startUri">The absolute <see cref="Uri"/> to start the scan from.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the ZAP Automation Framework plan contains more than a single URL in the "urls" section.
    /// </exception>
    public static YamlDocument SetStartUrl(this YamlDocument yamlDocument, Uri startUri)
    {
        // Setting includePaths in the context is not necessary because by default everything under "urls" will be
        // scanned.

        var urls = yamlDocument.GetUrls();
        var urlsCount = urls.Count();

        if (urlsCount > 1)
        {
            throw new ArgumentException(
                "The context in the ZAP Automation Framework YAML file should contain at most a single URL in the \"urls\" section.");
        }

        if (urlsCount == 1) urls.Children.Clear();

        urls.Add(startUri.ToString());

        return yamlDocument;
    }

    /// <summary>
    /// Adds a URL to the "urls" section of the current context of the ZAP Automation Framework plan.
    /// </summary>
    /// <param name="uri">
    /// The <see cref="Uri"/> to add to the "urls" section of the current context of the ZAP Automation Framework
    /// plan.
    /// </param>
    public static YamlDocument AddUrl(this YamlDocument yamlDocument, Uri uri)
    {
        var urls = yamlDocument.GetUrls();
        urls.Add(uri.ToString());
        return yamlDocument;
    }

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
                "No job named \"spider\" found in the Automation Framework Plan. We can only add the ajaxSpider job " +
                "immediately after it.");

        var spiderIndex = jobs.Children.IndexOf(spiderJob);
        var spiderAjaxJob = YamlHelper.LoadDocument(AutomationFrameworkPlanFragmentsPaths.SpiderAjaxJobPath);
        jobs.Children.Insert(spiderIndex + 1, spiderAjaxJob.GetRootNode());

        return yamlDocument;
    }

    /// <summary>
    /// Adds one or more regex patterns to the ZAP Automation Framework plan's excludePaths config under the current
    /// context.
    /// </summary>
    /// <param name="excludePathsRegexPatterns">
    /// One or more regex patterns to be added to the ZAP Automation Framework plan's excludePaths config under the
    /// current context. These should be regex patterns that match the whole absolute URL, so something like ".*blog.*"
    /// to match /blog, /blog/my-post, etc.
    /// </param>
    public static YamlDocument AddExcludePathsRegex(this YamlDocument yamlDocument, params string[] excludePathsRegexPatterns)
    {
        var currentContext = yamlDocument.GetCurrentContext();

        if (!currentContext.Children.ContainsKey("excludePaths")) currentContext.Add("excludePaths", new YamlSequenceNode());

        var excludePaths = (YamlSequenceNode)currentContext["excludePaths"];
        foreach (var pattern in excludePathsRegexPatterns)
        {
            excludePaths.Add(pattern);
        }

        return yamlDocument;
    }

    /// <summary>
    /// Disable a certain ZAP passive scan rule for the whole scan in the ZAP Automation Framework plan. If you only
    /// want to disable a rule for a given page, use <see cref="AddAlertFilter(YamlDocument, string, int, string, bool)"/>
    /// instead.
    /// </summary>
    /// <param name="id">The ID of the rule. In the scan report, this is usually displayed as "Plugin Id".</param>
    /// <param name="name">
    /// The human-readable name of the rule. Not required to turn off the rule, and its value doesn't matter. It's just
    /// useful for the readability of the method call.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if no job with the type "passiveScan-config" is found in the Automation Framework Plan.
    /// </exception>
    public static YamlDocument DisablePassiveScanRule(this YamlDocument yamlDocument, int id, string name = "")
    {
        var passiveScanConfigJob = GetPassiveScanConfigJobOrThrow(yamlDocument);

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
    /// Disable a certain ZAP active scan rule for the whole scan in the ZAP Automation Framework plan. If you only want
    /// to disable a rule for a given page, use <see cref="AddAlertFilter(YamlDocument, string, int, string, bool)"/>
    /// instead.
    /// </summary>
    /// <param name="id">The ID of the rule. In the scan report, this is usually displayed as "Plugin Id".</param>
    /// <param name="name">
    /// The human-readable name of the rule. Not required to turn off the rule, and its value doesn't matter. It's just
    /// useful for the readability of the method call.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if no job with the type "activeScan" is found in the Automation Framework Plan, or if it doesn't have a
    /// policyDefinition property.
    /// </exception>
    public static YamlDocument DisableActiveScanRule(this YamlDocument yamlDocument, int id, string name = "")
    {
        var jobs = yamlDocument.GetJobs();

        var activeScanConfigJob =
            (YamlMappingNode)jobs.FirstOrDefault(job => (string)job["type"] == "activeScan") ??
            throw new ArgumentException(
                "No job with the type \"activeScan\" found in the Automation Framework Plan so the rule can't be added.");

        if (!activeScanConfigJob.Children.ContainsKey("policyDefinition"))
        {
            throw new ArgumentException("The \"activeScan\" job should contain a policyDefinition.");
        }

        var policyDefinition = (YamlMappingNode)activeScanConfigJob["policyDefinition"];

        if (!policyDefinition.Children.ContainsKey("rules")) policyDefinition.Add("rules", new YamlSequenceNode());

        var newRule = new YamlMappingNode
        {
            { "id", id.ToTechnicalString() },
            { "name", name },
            { "threshold", "off" },
        };

        ((YamlSequenceNode)policyDefinition["rules"]).Add(newRule);

        return yamlDocument;
    }

    /// <summary>
    /// Adds an <see href="https://www.zaproxy.org/docs/desktop/addons/alert-filters/">Alert Filter</see> to the ZAP
    /// Automation Framework plan.
    /// </summary>
    /// <param name="ruleId">The ID of the rule. In the scan report, this is usually displayed as "Plugin Id".</param>
    /// <param name="urlMatchingRegexPattern">
    /// A regular expression pattern to match URLs against. This should be a regex pattern that matches the whole
    /// absolute URL, so something like ".*blog.*" to match /blog, /blog/my-post, etc.
    /// </param>
    /// <param name="ruleName">
    /// The human-readable name of the rule. Not required to turn off the rule, and its value doesn't matter. It's just
    /// useful for the readability of the method call.
    /// </param>
    /// <param name="isFalsePositive">
    /// If you disable the rule because it's a false positive, then set this to <see langword="true"/>. This helps the
    /// development of ZAP by collecting which rules have the highest false positive rate (see <see
    /// href="https://www.zaproxy.org/faq/how-do-i-handle-a-false-positive/"/>).
    /// </param>
    public static YamlDocument AddAlertFilter(
        this YamlDocument yamlDocument,
        string urlMatchingRegexPattern,
        int ruleId,
        string ruleName = "",
        bool isFalsePositive = false)
    {
        var jobs = yamlDocument.GetJobs();

        if (jobs.FirstOrDefault(job => (string)job["type"] == "alertFilter") is not YamlMappingNode alertFilterJob)
        {
            alertFilterJob = new YamlMappingNode
            {
                { "type", "alertFilter" },
                { "name", "alertFilter" },
            };

            var passiveScanConfigJob = GetPassiveScanConfigJobOrThrow(yamlDocument);
            var passiveScanConfigIndex = jobs.Children.IndexOf(passiveScanConfigJob);
            jobs.Children.Insert(passiveScanConfigIndex + 1, alertFilterJob);
        }

        if (!alertFilterJob.Children.ContainsKey("alertFilters")) alertFilterJob.Add("alertFilters", new YamlSequenceNode());

        var newRule = new YamlMappingNode
        {
            { "ruleId", ruleId.ToTechnicalString() },
            { "ruleName", ruleName },
            { "url", urlMatchingRegexPattern },
            { "urlRegex", "true" },
            { "newRisk", isFalsePositive ? "False Positive" : "Info" },
        };

        ((YamlSequenceNode)alertFilterJob["alertFilters"]).Add(newRule);

        return yamlDocument;
    }

    /// <summary>
    /// Adds a "requestor" job to the ZAP Automation Framework plan.
    /// </summary>
    /// <param name="url">The URL the requestor job will access.</param>
    /// <exception cref="ArgumentException">
    /// If no job named "spider" is found in the ZAP Automation Framework plan.
    /// </exception>
    public static YamlDocument AddRequestor(this YamlDocument yamlDocument, string url)
    {
        var jobs = yamlDocument.GetJobs();

        var spiderJob =
            jobs.FirstOrDefault(job => (string)job["name"] == "spider") ??
            throw new ArgumentException(
                "No job named \"spider\" found in the Automation Framework Plan. We can only add the requestor job " +
                "immediately before it.");

        var requestorJob = YamlHelper.LoadDocument(AutomationFrameworkPlanFragmentsPaths.RequestorJobPath).GetRootNode();

        ((YamlScalarNode)((YamlSequenceNode)requestorJob["requests"]).Children[0]["url"]).Value = url;

        var spiderIndex = jobs.Children.IndexOf(spiderJob);
        jobs.Children.Insert(spiderIndex, requestorJob);

        return yamlDocument;
    }

    /// <summary>
    /// Gets <see cref="YamlDocument.RootNode"/> cast to <see cref="YamlMappingNode"/>.
    /// </summary>
    public static YamlMappingNode GetRootNode(this YamlDocument yamlDocument) => (YamlMappingNode)yamlDocument.RootNode;

    /// <summary>
    /// Gets the "jobs" section of the ZAP Automation Framework plan.
    /// </summary>
    public static YamlSequenceNode GetJobs(this YamlDocument yamlDocument) =>
        (YamlSequenceNode)yamlDocument.GetRootNode()["jobs"];

    /// <summary>
    /// Gets the "urls" section of the current context in the ZAP Automation Framework plan.
    /// </summary>
    public static YamlSequenceNode GetUrls(this YamlDocument yamlDocument)
    {
        var currentContext = yamlDocument.GetCurrentContext();

        if (!currentContext.Children.ContainsKey("urls")) currentContext.Add("urls", new YamlSequenceNode());

        return (YamlSequenceNode)currentContext["urls"];
    }

    /// <summary>
    /// Gets the first context or the one named "Default Context" from the ZAP Automation Framework plan.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if the ZAP Automation Framework plan doesn't contain a context.
    /// </exception>
    public static YamlMappingNode GetCurrentContext(this YamlDocument yamlDocument)
    {
        var contexts = (YamlSequenceNode)yamlDocument.GetRootNode()["env"]["contexts"];

        if (!contexts.Any())
        {
            throw new ArgumentException(
                "The supplied ZAP Automation Framework plan YAML file should contain at least one context.");
        }

        var currentContext = (YamlMappingNode)contexts[0];

        if (contexts.Count() > 1)
        {
            currentContext = (YamlMappingNode)contexts.FirstOrDefault(context => context["Name"].ToString() == "Default Context")
                ?? currentContext;
        }

        return currentContext;
    }

    /// <summary>
    /// Shortcuts to <see cref="Task.CompletedTask"/> to be able to chain <see cref="YamlDocument"/> extensions in an
    /// async method/delegate.
    /// </summary>
    /// <returns><see cref="Task.CompletedTask"/>.</returns>
    public static Task CompletedTaskAsync(this YamlDocument yamlDocument) => Task.CompletedTask;

    /// <summary>
    /// Merge the given <see cref="YamlDocument"/> into the current one.
    /// </summary>
    /// <param name="overrideDocument">
    /// The <see cref="YamlDocument"/> to merge from, which overrides the current one.
    /// </param>
    /// <returns>The merged <see cref="YamlDocument"/>.</returns>
    public static YamlDocument MergeFrom(this YamlDocument baseDocument, YamlDocument overrideDocument)
    {
        var baseMapping = baseDocument.GetRootNode();
        var overrideMapping = overrideDocument.GetRootNode();

        foreach (var entry in overrideMapping.Children)
        {
            if (baseMapping.Children.ContainsKey(entry.Key))
            {
                // Override existing property.
                baseMapping.Children[entry.Key] = entry.Value;
            }
            else
            {
                // Add new property.
                baseMapping.Children.Add(entry.Key, entry.Value);
            }
        }

        return baseDocument;
    }

    private static YamlMappingNode GetPassiveScanConfigJobOrThrow(YamlDocument yamlDocument)
    {
        var jobs = yamlDocument.GetJobs();

        return (YamlMappingNode)jobs.FirstOrDefault(job => (string)job["type"] == "passiveScan-config") ??
            throw new ArgumentException(
                "No job with the type \"passiveScan-config\" found in the Automation Framework Plan so the rule can't be added.");
    }
}
