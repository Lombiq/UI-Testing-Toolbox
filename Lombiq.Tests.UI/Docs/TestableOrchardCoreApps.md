# Making an Orchard Core app testable



Tips on making specific features testable are under the ["Creating tests" page](CreatingTests.md) but here are some tips for the whole Orchard Core app you want to test. For an example of a UI-tested Orchard Core application see [Lombiq's Open-Source Orchard Core Extensions](https://github.com/Lombiq/Open-Source-Orchard-Core-Extensions).

**Note** that certain features of the Lombiq UI Testing Toolbox need to be enabled from test code in addition to making the app testable. Check out `OrchardCoreUITestExecutorConfiguration` for that part of the configuration; this page is only about changes necessary in the app.

- Create recipes with test content, and import them by starting with a UI testing-specific setup recipe. While you can run tests from an existing database, using recipes to create a test environment (that almost entirely doubles as a development environment) is more reliable. Keep in mind, that the data you test shouldn't change randomly, you can't assert on data coming from the export of a production app which is updated all the time. Using [Auto Setup](https://docs.orchardcore.net/en/dev/docs/reference/modules/AutoSetup/) works too, just check out the [samples project](../../Lombiq.Tests.UI.Samples/Readme.md).
- In your web project do the following:
  1. Add a reference to `Lombiq.Tests.UI.AppExtensions` (either from NuGet or as a Git submodule).
  2. Allow configuration of the app when launched for testing with the following piece of code in the app's `Startup` class:
        ```csharp
        public void ConfigureServices(IServiceCollection services)
        {
            // _configuration is a constructor-injected IConfiguration instance.
            services.AddOrchardCms(builder => builder.ConfigureUITesting(_configuration));
        }
        ``` 
- If you make use of shortcuts then add the `Lombiq.Tests.UI.Shortcuts` project (either from NuGet or as a Git submodule) as a reference to the root app project and enable it during UI testing by modifying the startup code as below (note the second `true` parameter):
    ```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        // _configuration is a constructor-injected IConfiguration instance.
        services.AddOrchardCms(builder => builder.ConfigureUITesting(_configuration, true));
    }
    ``` 
  Note that since authentication-related shortcuts need an implementation available for `Microsoft.AspNetCore.Identity.IRoleStore<OrchardCore.Security.IRole>` the `OrchardCore.Roles` feature will also be enabled. If you want to use a different implementation then enable the Shortcuts module from the test recipe, as well as the alternative Roles implementation, as below:
    ```json
    "steps": [
        {
            "name": "feature",
            "enable": [
                "Lombiq.Tests.UI.Shortcuts"
            ]
        }
    ]
    ```
- Tests should be self-contained and they shouldn't rely on any external dependencies like APIs or CDNs. It should be possible to run the app completely offline.
    - For static resources always provide local copies and make the CDN optional. Also disable CDN usage in the setup recipe:
    ```json
    "steps": [
        {
            "name": "settings",
            "UseCdn": false
        }
    ]
    ```
    - For external web APIs you can implement mock API services in features only enabled in tests. Those features you can again enable in a test recipe. An alternative is to use a tool that provides fake APIs like [JSON Server](https://github.com/typicode/json-server) or [Fake JSON Server](https://github.com/ttu/dotnet-fake-json-server). You can run such tools from the command line in the test's code, e.g. with the excellent [CliWrap](https://github.com/Tyrrrz/CliWrap) that the UI Testing Toolbox uses too.
- By default, the culture settings used when setting up an Orchard site depend on the host machine's culture. You want to make these settings consistent across all environments though so e.g. datetime and number formatting will be consistent. You can do this by enabling `OrchardCore.Localization` and configuring the culture in site settings from the setup recipe:
    ```json
    "steps": [
        {
            "name": "settings",
            // To make sure that e.g. numbers and dates are formatted the same way on all machines we have to specify the
            // culture too.
            "LocalizationSettings": {
                "DefaultCulture": "en-US",
                "SupportedCultures": [
                    "en-US"
                ]
            }
        }
    ]
    ```
- Some features send out e-mails. You can test them with the Lombiq UI Testing Toolbox's built-in feature to run an isolated local SMTP server with a web UI. The `OrchardCore.Email` feature will be automatically enabled, as well as the rest of the configuration applied.
- If you want the site to use Azure Blob Storage then you have to do the following:
  - The `OrchardCore.Media.Azure` feature will be automatically enabled, as well as the rest of the configuration applied.
  - It's recommended that you use the [Azurite emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azurite) as the storage for tests, not a real Azure Blob Storage resource. This is used by the UI Testing Toolbox by default. Be sure that it's running when the tests are executing.
  - If you want to use Blob Storage during local development then you can also configure it in your app's _appsettings.json_ or _appsettings.Development.json_ file like below but this is not necessary for UI testing:
    ```json
    {
      "OrchardCore": {
        "OrchardCore_Media_Azure": {
          "ConnectionString": "UseDevelopmentStorage=true",
          "ContainerName": "media",
          "CreateContainer": true
        }
      }
    }
    ```
