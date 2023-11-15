# Security scanning with ZAP

## Overview

You an create detailed security scans of your app with [Zed Attack Proxy (ZAP)](https://www.zaproxy.org/) right from the Lombiq UI Testing Toolbox, with detailed reports.

![Sample ZAP security scan report](Attachments/ZapReportScreenshot.png)

- `Lombiq.Tests.UI.Samples` contains a demonstration of how to use security scanning. Check that out for code examples.
- The most important default ZAP scans, Baseline, Full, GraphQL, and OpenAPI scans are included and readily usable.
- Since we use [ZAP's Automation Framework](https://www.zaproxy.org/docs/automate/automation-framework/) for configuration, you have complete and detailed control over how the scans are configured.
- [SARIF](https://sarifweb.azurewebsites.net/) reports are available to integrate with other InfoSec tools.
