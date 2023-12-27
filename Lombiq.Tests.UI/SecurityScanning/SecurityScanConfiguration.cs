using Lombiq.HelpfulLibraries.OrchardCore.Mvc;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using Lombiq.Tests.UI.Shortcuts.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;

namespace Lombiq.Tests.UI.SecurityScanning;

/// <summary>
/// High-level configuration for a security scan with <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see>.
/// </summary>
/// <remarks>
/// <para>
/// This class and <see cref="YamlDocumentExtensions"/> intentionally use different terminology, the latter assuming you
/// know ZAP. Here, we provide a simplified configuration for people who just want to use security scans without having
/// to understand ZAP's configuration too much.
/// </para>
/// </remarks>
public class SecurityScanConfiguration
{
    public Uri StartUri { get; private set; }
    public IList<Uri> AdditionalUris { get; } = new List<Uri>();
    public bool AjaxSpiderIsUsed { get; private set; }
    public string SignInUserName { get; private set; }
    public IList<string> ExcludedUrlRegexPatterns { get; } = new List<string>();
    public IList<ScanRule> DisabledActiveScanRules { get; } = new List<ScanRule>();
    public IDictionary<ScanRule, (ScanRuleThreshold Threshold, ScanRuleStrength Strength)> ConfiguredActiveScanRules { get; } =
        new Dictionary<ScanRule, (ScanRuleThreshold, ScanRuleStrength)>();
    public IList<ScanRule> DisabledPassiveScanRules { get; } = new List<ScanRule>();
    public IList<(string Url, int Id, string RuleName)> DisabledRulesForUrls { get; } = new List<(string Url, int Id, string RuleName)>();
    public IList<(string Url, int Id, string Justification)> FalsePositives { get; } = new List<(string Url, int Id, string Justification)>();
    public IList<Func<YamlDocument, Task>> ZapPlanModifiers { get; } = new List<Func<YamlDocument, Task>>();

    internal SecurityScanConfiguration()
    {
    }

    /// <summary>
    /// Sets the start URL under the app where to start the scan from.
    /// </summary>
    /// <param name="startUri">The <see cref="Uri"/> under the app where to start the scan from.</param>
    public SecurityScanConfiguration StartAtUri(Uri startUri)
    {
        StartUri = startUri;
        return this;
    }

    /// <summary>
    /// Adds an additional URL to visit during the scan. This is useful if you want to scan URLs that are otherwise
    /// unreachable from <see cref="StartUri"/>.
    /// </summary>
    /// <param name="additionalUri">The <see cref="Uri"/> under the app to also cover during the scan.</param>
    public SecurityScanConfiguration AddAdditionalUri(Uri additionalUri)
    {
        AdditionalUris.Add(additionalUri);
        return this;
    }

    /// <summary>
    /// Enables the <see href="https://www.zaproxy.org/docs/desktop/addons/ajax-spider/automation/">ZAP Ajax
    /// Spider</see>. This is useful if you have an SPA; it unnecessarily slows down the scan otherwise.
    /// </summary>
    public SecurityScanConfiguration UseAjaxSpider()
    {
        AjaxSpiderIsUsed = true;
        return this;
    }

    /// <summary>
    /// Signs in directly (see <see cref="AccountController.SignInDirectly(string)"/>) with the given user at the start
    /// of the scan.
    /// </summary>
    /// <param name="userName">The name of the user to sign in with directly.</param>
    public SecurityScanConfiguration SignIn(string userName = DefaultUser.UserName)
    {
        SignInUserName = userName;
        return this;
    }

    /// <summary>
    /// Excludes URLs from the scan that are matched by the supplied regex.
    /// </summary>
    /// <param name="excludedUrlRegex">
    /// The regex pattern to match URLs against. It will be matched against the whole absolute URL, e.g., ".*blog.*"
    /// will match https://example.com/blog, https://example.com/blog/my-post, etc.
    /// </param>
    public SecurityScanConfiguration ExcludeUrlWithRegex(string excludedUrlRegex)
    {
        ExcludedUrlRegexPatterns.Add(excludedUrlRegex);
        return this;
    }

    /// <summary>
    /// Disable a certain active scan rule for the whole scan. If you only want to disable a rule for specific pages
    /// matched by a regex, use <see cref="DisableScanRuleForUrlWithRegex(string, int, string)"/> instead.
    /// </summary>
    /// <param name="id">The ID of the rule. In the scan report, this is usually displayed as "Plugin Id".</param>
    /// <param name="name">
    /// The human-readable name of the rule. Not required to turn off the rule, and its value doesn't matter. It's just
    /// useful for the readability of the method call.
    /// </param>
    public SecurityScanConfiguration DisableActiveScanRule(int id, string name = "")
    {
        DisabledActiveScanRules.Add(new ScanRule(id, name));
        return this;
    }

    /// <summary>
    /// Configures a certain active scan rule for the whole scan.
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
    /// The human-readable name of the rule. Not required to turn off the rule, and its value doesn't matter. It's just
    /// useful for the readability of the method call.
    /// </param>
    public SecurityScanConfiguration ConfigureActiveScanRule(int id, ScanRuleThreshold threshold, ScanRuleStrength strength, string name = "")
    {
        ConfiguredActiveScanRules.Add(new ScanRule(id, name), (threshold, strength));
        return this;
    }

    /// <summary>
    /// Disable a certain passive scan rule for the whole scan. If you only want to disable a rule for specific pages
    /// matched by a regex, use <see cref="DisableScanRuleForUrlWithRegex(string, int, string)"/> instead.
    /// </summary>
    /// <param name="id">The ID of the rule. In the scan report, this is usually displayed as "Plugin Id".</param>
    /// <param name="name">
    /// The human-readable name of the rule. Not required to turn off the rule, and its value doesn't matter. It's just
    /// useful for the readability of the method call.
    /// </param>
    public SecurityScanConfiguration DisablePassiveScanRule(int id, string name = "")
    {
        DisabledPassiveScanRules.Add(new ScanRule(id, name));
        return this;
    }

    /// <summary>
    /// Disables a rule (can be any rule, including e.g. both active or passive scan rules) for just URLs matching the
    /// given regular expression pattern.
    /// </summary>
    /// <param name="urlRegex">
    /// The regex pattern to match URLs against. It will be matched against the whole absolute URL, e.g., ".*blog.*"
    /// will match https://example.com/blog, https://example.com/blog/my-post, etc.
    /// </param>
    /// <param name="ruleId">The ID of the rule. In the scan report, this is usually displayed as "Plugin Id".</param>
    /// <param name="ruleName">
    /// The human-readable name of the rule. Not required to turn off the rule, and its value doesn't matter. It's just
    /// useful for the readability of the method call.
    /// </param>
    public SecurityScanConfiguration DisableScanRuleForUrlWithRegex(string urlRegex, int ruleId, string ruleName = "")
    {
        DisabledRulesForUrls.Add((urlRegex, ruleId, ruleName));
        return this;
    }

    /// <summary>
    /// Marks a rule (can be any rule, including e.g. both active or passive scan rules) for just URLs matching the
    /// given regular expression pattern.
    /// </summary>
    /// <param name="urlRegex">
    /// The regex pattern to match URLs against. It will be matched against the whole absolute URL, e.g., ".*blog.*"
    /// will match https://example.com/blog, https://example.com/blog/my-post, etc.
    /// </param>
    /// <param name="ruleId">The ID of the rule. In the scan report, this is usually displayed as "Plugin Id".</param>
    /// <param name="justification">
    /// A human-readable explanation of why the alert is false positive.
    /// </param>
    public SecurityScanConfiguration MarkScanRuleAsFalsePositiveForUrlWithRegex(string urlRegex, int ruleId, string justification)
    {
        if (string.IsNullOrWhiteSpace(justification))
        {
            throw new InvalidOperationException("Please provide a justification for disabling this alert!");
        }

        FalsePositives.Add((urlRegex, ruleId, justification));
        return this;
    }

    /// <summary>
    /// Modifies the <see href="https://www.zaproxy.org/docs/automate/automation-framework/">Automation Framework</see>
    /// plan of <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see>, the tool used for the security scan.
    /// You can use this to do any arbitrary ZAP configuration.
    /// </summary>
    /// <param name="modifyPlan">
    /// A delegate to modify the deserialized representation of the <see
    /// href="https://www.zaproxy.org/docs/automate/automation-framework/">ZAP Automation Framework</see> plan in YAML.
    /// </param>
    public SecurityScanConfiguration ModifyZapPlan(Func<YamlDocument, Task> modifyPlan)
    {
        ZapPlanModifiers.Add(modifyPlan);
        return this;
    }

    /// <summary>
    /// Modifies the <see href="https://www.zaproxy.org/docs/automate/automation-framework/">Automation Framework</see>
    /// plan of <see href="https://www.zaproxy.org/">Zed Attack Proxy (ZAP)</see>, the tool used for the security scan.
    /// You can use this to do any arbitrary ZAP configuration.
    /// </summary>
    /// <param name="modifyPlan">
    /// A delegate to modify the deserialized representation of the <see
    /// href="https://www.zaproxy.org/docs/automate/automation-framework/">ZAP Automation Framework</see> plan in YAML.
    /// </param>
    public SecurityScanConfiguration ModifyZapPlan(Action<YamlDocument> modifyPlan)
    {
        ZapPlanModifiers.Add(yamlDocument =>
        {
            modifyPlan(yamlDocument);
            return Task.CompletedTask;
        });

        return this;
    }

    internal async Task ApplyToPlanAsync(YamlDocument yamlDocument, UITestContext context)
    {
        yamlDocument.SetStartUrl(StartUri);

        foreach (var uri in AdditionalUris) yamlDocument.AddUrl(uri);

        if (AjaxSpiderIsUsed) yamlDocument.AddSpiderAjaxAfterSpider();

        if (!string.IsNullOrEmpty(SignInUserName))
        {
            yamlDocument.AddRequestor(
                context.GetAbsoluteUri(
                    context.GetRelativeUrlOfAction<AccountController>(controller => controller.SignInDirectly(SignInUserName)))
                .ToString());

            // With such direct sign in we don't need to utilize ZAP's authentication and user managements mechanisms
            // (see https://www.zaproxy.org/docs/desktop/start/features/authmethods/ and
            // https://www.zaproxy.org/docs/desktop/addons/automation-framework/authentication/). If using the standard
            // Orchard Core login screen, that would also require using a (headless) browser, bringing all kinds of
            // WebDriver compatibility issues we've already solved here.

            // Also, it might be that later such a verification for the login state will need to be needed, but this
            // seems unnecessary now.
            // verification:
            //   method: "response"
            //   method: "poll"
            //   loggedInRegex: "Unauthenticated"
            //   loggedOutRegex: "UserName: .*"
            //   pollFrequency: 60
            //   pollUnits: "requests"
            //   pollUrl: "https://localhost:44335/Lombiq.Tests.UI.Shortcuts/CurrentUser/Index"
            //   pollPostData: ""
        }

        yamlDocument.AddExcludePathsRegex(ExcludedUrlRegexPatterns.ToArray());
        foreach (var rule in DisabledActiveScanRules) yamlDocument.DisableActiveScanRule(rule.Id, rule.Name);

        foreach (var ruleConfiguration in ConfiguredActiveScanRules)
        {
            yamlDocument.ConfigureActiveScanRule(
                ruleConfiguration.Key.Id,
                ruleConfiguration.Value.Threshold,
                ruleConfiguration.Value.Strength,
                ruleConfiguration.Key.Name);
        }

        foreach (var rule in DisabledPassiveScanRules) yamlDocument.DisablePassiveScanRule(rule.Id, rule.Name);
        foreach (var (url, id, name) in DisabledRulesForUrls) yamlDocument.AddAlertFilter(url, id, name);
        foreach (var (url, id, justification) in FalsePositives) yamlDocument.AddAlertFilter(url, id, justification, isFalsePositive: true);
        foreach (var modifier in ZapPlanModifiers) await modifier(yamlDocument);
    }

    public class ScanRule
    {
        public int Id { get; }
        public string Name { get; }

        public ScanRule(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
