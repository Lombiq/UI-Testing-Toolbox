# Security scanning with ZAP

## Overview

You an create detailed security scans of your app with [Zed Attack Proxy (ZAP)](https://www.zaproxy.org/) right from the Lombiq UI Testing Toolbox, with nice reports. ZAP is the world's most widely used web app security scanner, and a fellow open-source project we can recommend.

![Sample ZAP security scan report](Attachments/ZapReportScreenshot.png)

- The most important default ZAP scans, Baseline, Full, GraphQL, and OpenAPI scans are included and readily usable.
- You can assert on scan results and thus fail the test if there are security warnings.
- Since we use [ZAP's Automation Framework](https://www.zaproxy.org/docs/automate/automation-framework/) for configuration, you have complete and detailed control over how the scans are configured.
- [SARIF](https://sarifweb.azurewebsites.net/) reports are available to integrate with other InfoSec tools.

## Working with ZAP in the Lombiq UI Testing Toolbox

- We recommend you first check out the related samples in the [`Lombiq.Tests.UI.Samples` project](../../Lombiq.Tests.UI.Samples).
- If you're new to ZAP, you can start learning by checking out [ZAP's getting started guide](https://www.zaproxy.org/getting-started/), as well as the [ZAP Chat Videos](https://www.zaproxy.org/zap-chat/).
- ZAP scans run with an internally managed browser instance, not in the browser launched by the test.
- While ZAP is fully managed for you, Docker needs to be available and running to host the ZAP instance. On your development machine you can install [Docker Desktop](https://www.docker.com/products/docker-desktop/).
- The scan of a website with even just 1-200 pages can take 15-30 minutes. So, be careful to fine-tune the ZAP configuration to make it suitable for your app.

## Troubleshooting


