# Lombiq UI Testing Toolbox for Orchard Core



[![Lombiq.Tests.UI NuGet](https://img.shields.io/nuget/v/Lombiq.Tests.UI?label=Lombiq.Tests.UI)](https://www.nuget.org/packages/Lombiq.Tests.UI/)
[![Lombiq.Tests.UI.AppExtensions NuGet](https://img.shields.io/nuget/v/Lombiq.Tests.UI.AppExtensions?label=Lombiq.Tests.UI.AppExtensions)](https://www.nuget.org/packages/Lombiq.Tests.UI.AppExtensions/)
[![Lombiq.Tests.UI.Shortcuts NuGet](https://img.shields.io/nuget/v/Lombiq.Tests.UI.Shortcuts?label=Lombiq.Tests.UI.Shortcuts)](https://www.nuget.org/packages/Lombiq.Tests.UI.Shortcuts/)


## About

Web UI testing toolbox mostly for Orchard Core applications. Everything you need to do UI testing with Selenium for an Orchard app is here.

Highlights:

- Builds on proven libraries like Selenium, Atata, and xUnit. See all the tools we use [here](Lombiq.Tests.UI/Docs/Tools.md).
- Execute fully self-contained, repeatable, parallelizable automated UI tests on Orchard Core apps.
- Do cross-browser testing with all current browsers, both in normal and headless modes.
- Check the HTML structure and behavior of the app, check for errors in the Orchard logs and browser logs. Start troubleshooting from the detailed full application dumps and test logs if a test fails.
- Start tests with a setup using recipes, start with an existing Orchard Core app or take snapshots in between tests and resume from there. Use SQLite or SQL Server databases.
- Use local file storage or Azure Blob Storage for Media.
- Test e-mail sending with a local SMTP server too. Everything just works.
- Built-in tests to check that basic Orchard Core features work, like login, registration, and content management. Demo video [here](https://www.youtube.com/watch?v=jmhq63sRZrI).
- Built-in monkey testing functionality to try to break your app with random user interactions. Demo video [here](https://www.youtube.com/watch?v=pZbEsEz3tuE).
- Check for web content accessibility so people with disabilities can user your app properly too. You can also create accessibility reports for all pages.
- Check for the validity of the HTML markup either explicitly or automatically on all page changes.
- Reliability is built in, so you won't get false negatives.
- Use shortcuts for common Orchard Core operations like logging in or enabling features instead of going through the UI so you only test what you want, and it's also faster.
- Support for [TeamCity test metadata reporting](https://www.jetbrains.com/help/teamcity/reporting-test-metadata.html) so you can see the important details and metrics of a test at a glance in a TeamCity CI/CD server.

See a demo video of the project [here](https://www.youtube.com/watch?v=mEUg6-pad-E). Also, see our [Testing Toolbox](https://github.com/Lombiq/Testing-Toolbox) for similar features for lower-level tests.

Looking not just for dynamic testing but also static code analysis? Check out our [.NET Analyzers project](https://github.com/Lombiq/.NET-Analyzers).

Do you want to quickly try out this project and see it in action? Check it out in our [Open-Source Orchard Core Extensions](https://github.com/Lombiq/Open-Source-Orchard-Core-Extensions) full Orchard Core solution and also see our other useful Orchard Core-related open-source projects!


## Documentation


- [Tutorial and samples](Lombiq.Tests.UI.Samples/Readme.md)
- [Projects in the UI Testing Toolbox](Lombiq.Tests.UI/Docs/Projects.md)
- [Making an Orchard Core app testable](Lombiq.Tests.UI/Docs/TestableOrchardCoreApps.md)
- [Creating tests](Lombiq.Tests.UI/Docs/CreatingTests.md)
- [Configuration](Lombiq.Tests.UI/Docs/Configuration.md)
- [Executing tests](Lombiq.Tests.UI/Docs/ExecutingTests.md)
- [Troubleshooting](Lombiq.Tests.UI/Docs/Troubleshooting.md)
- [Limits](Lombiq.Tests.UI/Docs/Limits.md)
- [Tools we use](Lombiq.Tests.UI/Docs/Tools.md)


## Contributing and support

Bug reports, feature requests, comments, questions, code contributions, and love letters are warmly welcome, please do so via GitHub issues and pull requests. Please adhere to our [open-source guidelines](https://lombiq.com/open-source-guidelines) while doing so.

This project is developed by [Lombiq Technologies](https://lombiq.com/). Commercial-grade support is available through Lombiq.
