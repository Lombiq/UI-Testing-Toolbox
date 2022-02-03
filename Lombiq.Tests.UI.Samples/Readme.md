# Lombiq UI Testing Toolbox - Samples



Example UI testing project. The whole project is heavily documented to teach you how to write UI tests with the UI Testing Toolbox. It guides you through this process just like the [Lombiq Training Demo for Orchard Core](https://github.com/Lombiq/Orchard-Training-Demo-Module) teaches Orchard Core and Orchard 1 development.

For general details about and on using the Toolbox see the [root Readme](../Readme.md).


## Tutorial

1. Be sure to read the [root Readme](../Readme.md) first to understand how the UI Testing Toolbox works and how you can use it with an Orchard Core app.
2. The web app under test also needs a bit of configuration. For an example of this, check out [the `Startup` class of Lombiq's Open-Source Orchard Core Extensions](https://github.com/Lombiq/Open-Source-Orchard-Core-Extensions/blob/dev/src/Lombiq.OSOCE.Web/Startup.cs). This project assumes it's running in that solution.
3. Note that the project includes an _xunit.runner.json_ file. This is [xUnit's configuration file](https://xunit.net/docs/configuration-files). You don't necessarily need to include one if you're OK with [the default one](https://github.com/Lombiq/UI-Testing-Toolbox/blob/dev/Lombiq.Tests.UI/xunit.runner.json).
4. Now that we have the basics out of the way start the tutorial in the [*GlobalSuppressions.cs*](GlobalSuppressions.cs) file.


## Training sections

- [UI Testing Toolbox basics](GlobalSuppressions.cs)
- [Basic Orchard features tests](Tests/BasicOrchardFeaturesTests.cs)
- [E-mail tests](Tests/EmailTests.cs)
- [Accessibility tests](Tests/AccessibilityTest.cs)
- [Using SQL Server](Tests/SqlServerTests.cs)
- [Using Azure Blob Storage](Tests/AzureBlobStorageTests.cs)
- [Error handling](Tests/ErrorHandlingtests.cs)
- [Monkey tests](Tests/MonkeyTests.cs)


## Adding new tutorials

Follow the practices of the [Lombiq Training Demo for Orchard Core](https://github.com/Lombiq/Orchard-Training-Demo-Module#contributing-and-support).
