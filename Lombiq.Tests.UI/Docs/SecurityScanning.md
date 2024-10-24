# Security scanning with ZAP

## Overview

You can create detailed security scans of your app with [Zed Attack Proxy (ZAP)](https://www.zaproxy.org/) right from the Lombiq UI Testing Toolbox, with nice reports. ZAP is the world's most widely used web app security scanner, and a fellow open-source project we can recommend. See a demo video of this feature [here](https://www.youtube.com/watch?v=iUYivLkFbY4).

![Sample ZAP security scan report](Attachments/ZapReportScreenshot.png)

- The most important default ZAP scans, Baseline, Full, GraphQL, and OpenAPI scans are included and readily usable. Note that these are modified to be more applicable to Orchard Core apps run on localhost during a UI testing scenario (notably, [`ajaxSpider`](https://www.zaproxy.org/docs/desktop/addons/ajax-spider/automation/) is removed, since most Orchard Core apps don't need it but it takes a lot of time, and rules as well as selected technologies are adjusted). If you want to scan remote (and especially production) apps, then you'll need to create your own scans based on ZAP's default ones. These can then be run from inside UI tests too.
- You can assert on scan results and thus fail the test if there are security warnings.
- Since we use [ZAP's Automation Framework](https://www.zaproxy.org/docs/automate/automation-framework/) for configuration, you have complete and detailed control over how the scans are configured, but you can also start with a simple configuration available in the .NET API.
- [SARIF](https://sarifweb.azurewebsites.net/) reports are available to integrate with other InfoSec tools.

## Working with ZAP in the Lombiq UI Testing Toolbox

- We recommend you first check out the [related samples in the `Lombiq.Tests.UI.Samples` project](../../Lombiq.Tests.UI.Samples/Tests/SecurityScanningTests.cs).
- If you're new to ZAP, you can start learning by checking out [ZAP's getting started guide](https://www.zaproxy.org/getting-started/), as well as the [ZAP Chat Videos](https://www.zaproxy.org/zap-chat/). The [documentation on ZAP's Automation Framework](https://www.zaproxy.org/docs/automate/automation-framework/) and the [ZAP Chat 06 Automation Introduction video](https://www.youtube.com/watch?v=PnCbIAnauD8) (as well as the subsequent videos about it in the series) will help you understand what we use under the hood to instruct ZAP, and will allow you to use your completely custom Automation Framework plans too.
- Be aware that ZAP scans run its own spider or with an internally managed browser instance, not in the browser launched by the test.
- While ZAP is fully managed for you, Docker needs to be available and running to host the ZAP instance. On your development machine, you can install [Docker Desktop](https://www.docker.com/products/docker-desktop/).
- The full scan of a website with even just 1-200 pages can take 5-10 minutes. So, be careful to fine-tune the ZAP configuration to make it suitable for your app.

## Limitations

On Windows-based GitHub runners the security tests always fail with the following error:

> The `docker.exe pull softwaresecurityproject/zap-stable:2.14.0 --quiet` command failed with the output below. no matching manifest for windows/amd64 10.0.20348 in the manifest list entries.

This is because the Docker installation is configured to use Windows images, while the [ZAP docker image](https://hub.docker.com/r/softwaresecurityproject/zap-stable/tags) is only available for Linux. If you rely on our [Lombiq GitHub Actions](https://github.com/Lombiq/GitHub-Actions) then you can configure it like this to disable a test, in this case `SecurityScanningTests`:

```yaml
  build-and-test:
    name: Build and Test
    # See https://github.com/Lombiq/GitHub-Actions/blob/dev/.github/workflows/build-and-test-orchard-core.yml.
    uses: Lombiq/GitHub-Actions/.github/workflows/build-and-test-orchard-core.yml@dev
    with:
      machine-types: '["windows-latest"]'
      test-filter: "FullyQualifiedName!~SecurityScanningTests"
```

## Troubleshooting

- Most common alerts in Orchard Core can be resolved by [using the extension method in Lombiq Helpful Libraries](https://github.com/Lombiq/Helpful-Libraries/blob/dev/Lombiq.HelpfulLibraries.OrchardCore/Docs/Security.md) like this: `orchardCoreBuilder.ConfigureSecurityDefaults(allowInlineStyle: true)`.
- If you're unsure what happens in a scan, run the [ZAP desktop app](https://www.zaproxy.org/download/) and load the Automation Framework plan's YAML file into it. If you use the default scans, then these will be available under the build output directory (like _bin/Debug_) under _SecurityScanning/AutomationFrameworkPlans_. Then, you can open and run them as demonstrated [in this video](https://youtu.be/PnCbIAnauD8?si=u0vi63Uvv9wZINzb&t=1173).
- If an alert is a false positive, follow [the official docs](https://www.zaproxy.org/faq/how-do-i-handle-a-false-positive/). You can use the [`alertFilter` job](https://www.zaproxy.org/docs/desktop/addons/alert-filters/automation/) to ignore alerts in very specific conditions. You can also access this via the .NET configuration API via `SecurityScanConfiguration.MarkScanRuleAsFalsePositiveForUrlWithRegex()`. For less common filters (e.g., filters other than by URL), you can use the `SecurityScanConfiguration.ModifyZapPlan()` and `YamlDocument.AddFalsePositiveRuleFilter()` extension methods to configure the action filter YAML node directly (see `SecurityScanWithCustomConfigurationShouldPass` test in the sample).
- ZAP didn't find everything in your app? By default, ZAP has a crawl depth of 5 for its standard spider and 10 for its AJAX spider. Set `maxDepth` (and `maxChildren`) [for `spider`](https://www.zaproxy.org/docs/desktop/addons/automation-framework/job-spider/) and `maxCrawlDepth` [for `spiderAjax`](https://www.zaproxy.org/docs/desktop/addons/ajax-spider/automation/).
- Do you sometimes get slightly different scan results? This is normal, and ZAP can be inconsistent/appear random within limits, see [the official docs page](https://www.zaproxy.org/faq/why-can-zap-scans-be-inconsistent/).
- Is the active scan too slow?
  - You can find out which rules take the most time by adding a script displaying each rules' runtime with `YamlDocumentExtensions.AddDisplayActiveScanRuleRuntimesScriptAfterActiveScan()`.
  - The ["Cross Site Scripting (DOM Based)" active scan rule](https://www.zaproxy.org/docs/desktop/addons/dom-xss-active-scan-rule/), unlike other rules, launches browsers and thus will take 1-2 orders of magnitude more time than other scans, usually causing the bulk of an active scan's runtime. Also see [the official docs](https://www.zaproxy.org/docs/desktop/addons/dom-xss-active-scan-rule/). You can tune it so it completes faster but still produces acceptable results for your app. You can do this from the Automation Framework plan's YAML file (see the samples on how you can use a custom one), or with `SecurityScanConfiguration.ConfigureXssActiveScanRule()`.
  - In CI workflows, you might want to restrict how many scans run in parallel, if you have more than one. You can use [xUnit's `[Collection]` attributes](https://xunit.net/docs/running-tests-in-parallel#parallelism-in-test-frameworks) to have e.g. only two collections for such tests, thus allowing only two parallel scans.
- The test fails due to Orchard Core exceptions caused by ZAP being logged, but you have no idea how those happened?
  - If you app uses NLog for logging, make it log the URL too with the [AspNetRequest Url Layout Renderer](https://github.com/NLog/NLog/wiki/AspNetRequest-Url-Layout-Renderer): `${aspnet-request-url:IncludeQueryString=true}`. This will help you pinpoint the ZAP request that caused the exception. We recommend adding this before `${aspnet-traceidentifier}`, so the usual format of log messages is otherwise preserved.
  - Set `SecurityScanningConfiguration.CreateReportOnTestFailAlways` to `true` to get a report even if the security scan passes. In the report, you may find details even about ignored alerts.
  - Increase the ZAP log level to `Debug` via `SecurityScanningConfiguration.ZapLogLevel`. This will include the ZAP log in the failure dump, providing you with more information about what ZAP did exactly, including the URLs of the requests it sent. Note that this slows down the security scan considerably (can even double the runtime), so use it only when necessary.
