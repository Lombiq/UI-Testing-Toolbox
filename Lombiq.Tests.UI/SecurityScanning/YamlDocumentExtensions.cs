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
    public static YamlDocument SetStartUrl(this YamlDocument yamlDocument, Uri startUri)
    {
        // Setting includePaths in the context is not necessary because by default everything under "urls" will be
        // scanned.

        var urls = yamlDocument.GetUrls();
        urls.Children.Clear();
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
    /// to the ZAP Automation Framework plan, just after the job named "spider".
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if no job named "spider" is found in the ZAP Automation Framework plan.
    /// </exception>
    public static YamlDocument AddSpiderAjaxAfterSpider(this YamlDocument yamlDocument)
    {
        var jobs = yamlDocument.GetJobs();
        var spiderJob =
            yamlDocument.GetSpiderJob() ??
            throw new ArgumentException(
                "No job named \"spider\" found in the Automation Framework Plan. We can only add the ajaxSpider job " +
                "immediately after it.");

        var spiderIndex = jobs.Children.IndexOf(spiderJob);
        var spiderAjaxJob = YamlHelper.LoadDocument(AutomationFrameworkPlanFragmentsPaths.SpiderAjaxJobPath);
        jobs.Children.Insert(spiderIndex + 1, spiderAjaxJob.RootNode);

        return yamlDocument;
    }

    /// <summary>
    /// Adds a script to the ZAP Automation Framework plan that displays the runtime of each active scan rule, in milliseconds,
    /// just after the first job with the type "activeScan".
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if no job with the type "activeScan" is found in the ZAP Automation Framework plan.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Script code taken from <see href="https://groups.google.com/g/zaproxy-users/c/TxHqhnarpuU"/>.
    /// </para>
    /// </remarks>
    public static YamlDocument AddDisplayActiveScanRuleRuntimesScriptAfterActiveScan(this YamlDocument yamlDocument)
    {
        var jobs = yamlDocument.GetJobs();
        var activeScanJob =
            yamlDocument.GetJobByType("activeScan") ??
            throw new ArgumentException(
                "No job with the type \"activeScan\" found in the Automation Framework Plan. We can only add the " +
                "active scan rule runtime-displaying script immediately after it.");

        var activeScanIndex = jobs.Children.IndexOf(activeScanJob);
        var scriptJobs = ((YamlSequenceNode)YamlHelper
            .LoadDocument(AutomationFrameworkPlanFragmentsPaths.DisplayActiveScanRuleRuntimesScriptPath).RootNode)
            .Children;

        for (int i = scriptJobs.Count - 1; i >= 0; i--)
        {
            jobs.Children.Insert(activeScanIndex + 1, scriptJobs[i]);
        }

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

        var excludePaths = currentContext.GetOrAddNode<YamlSequenceNode>("excludePaths");
        foreach (var pattern in excludePathsRegexPatterns)
        {
            excludePaths.Add(pattern);
        }

        return yamlDocument;
    }

    /// <summary>
    /// Disable a certain ZAP passive scan rule for the whole scan in the ZAP Automation Framework plan. If you only
    /// want to disable a rule for a given page, use <see cref="AddDisableRuleFilter"/> instead.
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
        yamlDocument.GetPassiveScanConfigJobOrThrow().AddRuleToRulesNode(new YamlMappingNode
        {
            { "id", id.ToTechnicalString() },
            { "name", name },
            { "threshold", "off" },
        });

        return yamlDocument;
    }

    /// <summary>
    /// Disable a certain ZAP active scan rule for the whole scan in the ZAP Automation Framework plan. If you only want
    /// to disable a rule for a given page, use <see cref="AddDisableRuleFilter"/> instead.
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
    public static YamlDocument DisableActiveScanRule(this YamlDocument yamlDocument, int id, string name = "") =>
        yamlDocument.ConfigureActiveScanRule(id, ScanRuleThreshold.Off, ScanRuleStrength.Default, name);

    /// <summary>
    /// Configures a certain ZAP active scan rule for the whole scan in the ZAP Automation Framework plan.
    /// </summary>
    /// <param name="id">The ID of the rule. In the scan report, this is usually displayed as "Plugin Id".</param>
    /// <param name="threshold">
    /// Controls how likely ZAP is to report potential vulnerabilities. See <see
    /// href="https://www.zaproxy.org/docs/desktop/ui/dialogs/scanpolicy/#threshold">the official docs</see>.
    /// </param>
    /// <param name="strength">
    /// Controls the number of attacks that ZAP will perform. See <see
    /// href="https://www.zaproxy.org/docs/desktop/ui/dialogs/scanpolicy/#strength">the official docs</see>.
    /// </param>
    /// <param name="name">
    /// The human-readable name of the rule. Not required to configure the rule, and its value doesn't matter. It's just
    /// useful for the readability of the method call.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if no job with the type "activeScan" is found in the Automation Framework Plan, or if it doesn't have a
    /// policyDefinition property.
    /// </exception>
    public static YamlDocument ConfigureActiveScanRule(
        this YamlDocument yamlDocument,
        int id,
        ScanRuleThreshold threshold,
        ScanRuleStrength strength,
        string name = "")
    {
        var activeScanConfigJob =
            (YamlMappingNode)yamlDocument.GetJobByType("activeScan") ??
            throw new ArgumentException(
                "No job with the type \"activeScan\" found in the Automation Framework Plan so the rule can't be added.");

        if (!activeScanConfigJob.Children.ContainsKey("policyDefinition"))
        {
            throw new ArgumentException("The \"activeScan\" job should contain a policyDefinition.");
        }

        activeScanConfigJob["policyDefinition"].AddRuleToRulesNode(new YamlMappingNode
        {
            { "id", id.ToTechnicalString() },
            { "name", name },
            { "threshold", threshold.ToString() },
            { "strength", strength.ToString() },
        });

        return yamlDocument;
    }

    /// <summary>
    /// Adds an <see href="https://www.zaproxy.org/docs/desktop/addons/alert-filters/">Alert Filter</see> to the ZAP
    /// Automation Framework plan.
    /// </summary>
    public static YamlDocument AddAlertFilter(this YamlDocument yamlDocument, YamlMappingNode filter)
    {
        var jobs = yamlDocument.GetJobs();

        if (yamlDocument.GetJobByType("alertFilter") is not YamlMappingNode alertFilterJob)
        {
            alertFilterJob = new YamlMappingNode
            {
                { "type", "alertFilter" },
                { "name", "alertFilter" },
            };

            var passiveScanConfigJob = yamlDocument.GetPassiveScanConfigJobOrThrow();
            var passiveScanConfigIndex = jobs.Children.IndexOf(passiveScanConfigJob);
            jobs.Children.Insert(passiveScanConfigIndex + 1, alertFilterJob);
        }

        alertFilterJob.GetOrAddNode<YamlSequenceNode>("alertFilters").Add(filter);
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
    public static YamlDocument AddDisableRuleFilter(
        this YamlDocument yamlDocument,
        string urlMatchingRegexPattern,
        int ruleId,
        string ruleName,
        Action<YamlMappingNode> configureFilter = null)
    {
        var alertFilter = new YamlMappingNode
        {
            { "ruleId", ruleId.ToTechnicalString() },
            { "ruleName", ruleName },
            { "url", urlMatchingRegexPattern },
            { "urlRegex", "true" },
            { "newRisk", "Info" },
        };

        configureFilter?.Invoke(alertFilter);

        return yamlDocument.AddAlertFilter(alertFilter);
    }

    /// <inheritdoc cref="AddDisableRuleFilter"/>
    /// <param name="justification">
    /// An informational text explaining why the alert in question is false positive. This helps the development of ZAP
    /// by collecting which rules have the highest false positive rate (see <see
    /// href="https://www.zaproxy.org/faq/how-do-i-handle-a-false-positive/"/>).
    /// </param>
    public static YamlDocument AddFalsePositiveRuleFilter(
        this YamlDocument yamlDocument,
        string urlMatchingRegexPattern,
        int ruleId,
        string ruleName,
        string justification,
        Action<YamlMappingNode> configureFilter = null) =>
        yamlDocument.AddDisableRuleFilter(
            urlMatchingRegexPattern,
            ruleId, $"{ruleName}: {justification}",
            node =>
            {
                configureFilter?.Invoke(node);
                node.Children["newRisk"] = "False Positive";
            });

    /// <summary>
    /// Adds a "requestor" job to the ZAP Automation Framework plan just before the job named "spider".
    /// </summary>
    /// <param name="url">The URL the requestor job will access.</param>
    /// <exception cref="ArgumentException">
    /// If no job named "spider" is found in the ZAP Automation Framework plan.
    /// </exception>
    public static YamlDocument AddRequestor(this YamlDocument yamlDocument, string url)
    {
        var jobs = yamlDocument.GetJobs();

        var spiderJob =
            yamlDocument.GetSpiderJob() ??
            throw new ArgumentException(
                "No job named \"spider\" found in the Automation Framework Plan. We can only add the requestor job " +
                "immediately before it.");

        var requestorJob = YamlHelper.LoadDocument(AutomationFrameworkPlanFragmentsPaths.RequestorJobPath).GetRootNode();

        ((YamlSequenceNode)requestorJob["requests"]).Children[0]["url"].SetValue(url);

        var spiderIndex = jobs.Children.IndexOf(spiderJob);
        jobs.Children.Insert(spiderIndex, requestorJob);

        return yamlDocument;
    }

    /// <summary>
    /// Gets <see cref="YamlDocument.RootNode"/> cast to <see cref="YamlMappingNode"/>.
    /// </summary>
    public static YamlMappingNode GetRootNode(this YamlDocument yamlDocument) => (YamlMappingNode)yamlDocument.RootNode;

    /// <summary>
    /// Tries to access the child node by the name of <paramref name="key"/>. If it doesn't exists, it's created.
    /// </summary>
    public static T GetOrAddNode<T>(this YamlNode yamlNode, string key)
        where T : YamlNode, new()
    {
        if (yamlNode is YamlMappingNode mappingNode && !mappingNode.Children.ContainsKey(key))
        {
            mappingNode.Children.Add(key, new T());
        }

        return (T)yamlNode[key];
    }

    /// <summary>
    /// Adds the specified mapping to the <see cref="YamlMappingNode.Children" /> collection. If the given <paramref
    /// name="key"/> already exists, it's removed first.
    /// </summary>
    public static void SetMappingChild(this YamlMappingNode node, YamlNode key, YamlNode value)
    {
        if (node.Children.ContainsKey(key))
        {
            node.Children.Remove(key);
        }

        node.Add(key, value);
    }

    /// <summary>
    /// Gets the "jobs" section of the ZAP Automation Framework plan.
    /// </summary>
    public static YamlSequenceNode GetJobs(this YamlDocument yamlDocument) =>
        (YamlSequenceNode)yamlDocument.GetRootNode()["jobs"];

    /// <summary>
    /// Gets the job from the "jobs" section of the ZAP Automation Framework with the name "spider".
    /// </summary>
    public static YamlNode GetSpiderJob(this YamlDocument yamlDocument) => yamlDocument.GetJobByName("spider");

    /// <summary>
    /// Gets the job from the "jobs" section of the ZAP Automation Framework with the name "activeScan".
    /// </summary>
    public static YamlNode GetActiveScanJob(this YamlDocument yamlDocument) => yamlDocument.GetJobByName("activeScan");

    /// <summary>
    /// Gets or creates the "parameters" node under the specified <paramref name="jobNode"/>.
    /// </summary>
    public static YamlMappingNode GetOrCreateParameters(this YamlNode jobNode) =>
        jobNode.GetOrAddNode<YamlMappingNode>("parameters");

    /// <summary>
    /// Updates the provided configuration parameters for the "spider" job it it exists.
    /// </summary>
    public static void SetSpiderParameter(this YamlDocument yamlDocument, YamlNode parameter, YamlNode value) =>
        yamlDocument.GetSpiderJob()?.GetOrCreateParameters().SetMappingChild(parameter, value);

    /// <summary>
    /// Updates the provided configuration parameters for the "activeScan" job it it exists.
    /// </summary>
    public static void SetActiveScanParameter(this YamlDocument yamlDocument, YamlNode parameter, YamlNode value) =>
        yamlDocument.GetActiveScanJob()?.GetOrCreateParameters().SetMappingChild(parameter, value);

    /// <summary>
    /// Sets time limits on the "activeScan" job. Both are in minutes. If set to 0 it means unlimited.
    /// </summary>
    /// <param name="maxActiveScanDurationInMinutes">Time limit for the active scan altogether.</param>
    /// <param name="maxRuleDurationInMinutes">Time limit for the individual rule scans.</param>
    public static void SetActiveScanMaxDuration(
        this YamlDocument yamlDocument,
        int maxActiveScanDurationInMinutes,
        int maxRuleDurationInMinutes = 0)
    {
        yamlDocument.SetActiveScanParameter("maxScanDurationInMins", maxActiveScanDurationInMinutes.ToTechnicalString());
        yamlDocument.SetActiveScanParameter("maxRuleDurationInMins", maxRuleDurationInMinutes.ToTechnicalString());
    }

    /// <summary>
    /// Gets a job from the "jobs" section of the ZAP Automation Framework plan by its name.
    /// </summary>
    /// <param name="jobName">The "name" field of the job to search for.</param>
    public static YamlNode GetJobByName(this YamlDocument yamlDocument, string jobName) =>
        yamlDocument.GetJobs().FirstOrDefault(job => (string)job["name"] == jobName);

    /// <summary>
    /// Gets a job from the "jobs" section of the ZAP Automation Framework plan by its type.
    /// </summary>
    /// <param name="jobType">The "type" field of the job to search for.</param>
    public static YamlNode GetJobByType(this YamlDocument yamlDocument, string jobType) =>
        yamlDocument.GetJobs().FirstOrDefault(job => (string)job["type"] == jobType);

    /// <summary>
    /// Gets the "urls" section of the current context in the ZAP Automation Framework plan.
    /// </summary>
    public static YamlSequenceNode GetUrls(this YamlDocument yamlDocument)
    {
        var currentContext = yamlDocument.GetCurrentContext();

        return currentContext.GetOrAddNode<YamlSequenceNode>("urls");
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
    /// Gets the job with the type "passiveScan-config" from the ZAP Automation Framework plan.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if the ZAP Automation Framework plan doesn't contain a job with the type "passiveScan-config".
    /// </exception>
    public static YamlMappingNode GetPassiveScanConfigJobOrThrow(this YamlDocument yamlDocument) =>
        (YamlMappingNode)yamlDocument.GetJobByType("passiveScan-config") ??
            throw new ArgumentException(
                "No job with the type \"passiveScan-config\" found in the Automation Framework Plan.");

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

    /// <summary>
    /// Ensures that the <paramref name="parentOfRules"/> has a <c>rules</c> child and then adds the <paramref
    /// name="newRule"/> to it.
    /// </summary>
    public static void AddRuleToRulesNode(this YamlNode parentOfRules, YamlNode newRule) =>
        parentOfRules.GetOrAddNode<YamlSequenceNode>("rules").Add(newRule);
}
