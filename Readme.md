# Lombiq UI Testing Toolbox for Orchard Core

[![Lombiq.Tests.UI NuGet](https://img.shields.io/nuget/v/Lombiq.Tests.UI?label=Lombiq.Tests.UI)](https://www.nuget.org/packages/Lombiq.Tests.UI/) [![Lombiq.Tests.UI.AppExtensions NuGet](https://img.shields.io/nuget/v/Lombiq.Tests.UI.AppExtensions?label=Lombiq.Tests.UI.AppExtensions)](https://www.nuget.org/packages/Lombiq.Tests.UI.AppExtensions/) [![Lombiq.Tests.UI.Shortcuts NuGet](https://img.shields.io/nuget/v/Lombiq.Tests.UI.Shortcuts?label=Lombiq.Tests.UI.Shortcuts)](https://www.nuget.org/packages/Lombiq.Tests.UI.Shortcuts/)

## About

Web UI testing toolbox mostly for Orchard Core applications. Everything you need to do UI testing with Selenium for an Orchard app is here.

Highlights:

- Builds on proven libraries like Selenium, Atata, and xUnit. See all the tools we use [here](Lombiq.Tests.UI/Docs/Tools.md).
- Execute fully self-contained, repeatable, parallelizable automated UI tests on Orchard Core apps.
- Do cross-browser testing with all current browsers, both in normal and headless modes.
- Check the HTML structure and behavior of the app, check for errors in the Orchard logs and browser logs. Start troubleshooting from the detailed full application dumps and test logs if a test fails.
- Start tests with a setup using recipes, start with an existing Orchard Core app or take snapshots in between tests and resume from there. Use SQLite or SQL Server database snapshots (but you can also use PostgreSQL or MySQL too without snapshots, see [this proof of concept](https://github.com/OrchardCMS/OrchardCore/pull/11194/files)).
- Use local file storage or Azure Blob Storage for Media.
- Test e-mail sending with a local SMTP server too. Everything just works.
- Built-in tests to check that basic Orchard Core features work, like login, registration, and content management. Demo video [here](https://www.youtube.com/watch?v=jmhq63sRZrI). And a [demo video](https://www.youtube.com/watch?v=BwHoLmgrV9g) about a proof of concept to add UI testing to Orchard.
- Built-in monkey testing functionality to try to break your app with random user interactions. Demo video [here](https://www.youtube.com/watch?v=pZbEsEz3tuE).
- Check for web content accessibility so people with disabilities can use your app properly too. You can also create accessibility reports for all pages.
- Check for the validity of the HTML markup either explicitly or automatically on all page changes.
- Reliability is built in, so you won't get false negatives.
- Use shortcuts for common Orchard Core operations like logging in or enabling features instead of going through the UI so you only test what you want, and it's also faster.
- Ready to use GitHub Actions workflows for CI builds and support for test grouping and error annotations in [Lombiq GitHub Actions](https://github.com/features/actions). This feature is automatically enabled if a GitHub environment is detected.
- Support for [TeamCity test metadata reporting](https://www.jetbrains.com/help/teamcity/reporting-test-metadata.html) so you can see the important details and metrics of a test at a glance in a TeamCity CI/CD server.
- Visual verification testing: You can make the test fail if the look of the app changes. Demo video [here](https://www.youtube.com/watch?v=a-1zKjxTKkk).
- If your app uses a camera, a fake video capture source in Chrome is supported. [Here's a demo video of the feature](https://www.youtube.com/watch?v=sGcD0eJ2ytc), and check out the docs [here](Lombiq.Tests.UI/Docs/FakeVideoCaptureSource.md).

See a demo video of the project [here](https://www.youtube.com/watch?v=mEUg6-pad-E), and the Orchard Harvest 2023 conference talk about automated QA in Orchard Core [here](https://youtu.be/CHdhwD2NHBU). Also, see our [Testing Toolbox](https://github.com/Lombiq/Testing-Toolbox) for similar features for lower-level tests.

Looking not just for dynamic testing but also static code analysis? Check out our [.NET Analyzers project](https://github.com/Lombiq/.NET-Analyzers).

We at [Lombiq](https://lombiq.com/) also used this module for the following projects:

- The new [City of Santa Monica website](https://santamonica.gov/) when migrating it from Orchard 1 to Orchard Core ([see case study](https://lombiq.com/blog/helping-the-city-of-santa-monica-with-orchard-core-consulting)).
- The new [Smithsonian Folkways Recordings website](https://folkways.si.edu/) when migrating it from Orchard 1 to Orchard Core ([see case study](https://lombiq.com/blog/smithsonian-folkways-recordings-now-upgraded-to-orchard-core)).
- The new [Lombiq website](https://lombiq.com/) when migrating it from Orchard 1 to Orchard Core ([see case study](https://lombiq.com/blog/how-we-renewed-and-migrated-lombiq-com-from-orchard-1-to-orchard-core)).
- It also makes [DotNest, the Orchard SaaS](https://dotnest.com/) better.

Do you want to quickly try out this project and see it in action? Check it out in our [Open-Source Orchard Core Extensions](https://github.com/Lombiq/Open-Source-Orchard-Core-Extensions) full Orchard Core solution and also see our other useful Orchard Core-related open-source projects!

## Documentation

- [Tutorial and samples](Lombiq.Tests.UI.Samples/Readme.md)
- [Projects in the UI Testing Toolbox](Lombiq.Tests.UI/Docs/Projects.md)
- [Making an Orchard Core app testable](Lombiq.Tests.UI/Docs/TestableOrchardCoreApps.md)
- [Creating tests](Lombiq.Tests.UI/Docs/CreatingTests.md)
- [Configuration](Lombiq.Tests.UI/Docs/Configuration.md)
- [Executing tests](Lombiq.Tests.UI/Docs/ExecutingTests.md)
- [Fake video capture source](Lombiq.Tests.UI/Docs/FakeVideoCaptureSource.md)
- [Troubleshooting](Lombiq.Tests.UI/Docs/Troubleshooting.md)
- [Limits](Lombiq.Tests.UI/Docs/Limits.md)
- [Tools we use](Lombiq.Tests.UI/Docs/Tools.md)
- [Linux-specific considerations](Lombiq.Tests.UI/Docs/Linux.md)
- [Version migration guide](Lombiq.Tests.UI/Docs/Migration.md)

## Contributing and support

Bug reports, feature requests, comments, questions, code contributions and love letters are warmly welcome. You can send them to us via GitHub issues and pull requests. Please adhere to our [open-source guidelines](https://lombiq.com/open-source-guidelines) while doing so.

This project is developed by [Lombiq Technologies](https://lombiq.com/). Commercial-grade support is available through Lombiq.
