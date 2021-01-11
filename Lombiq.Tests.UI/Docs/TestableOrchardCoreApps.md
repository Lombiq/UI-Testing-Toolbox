# Making an Orchard Core app testable



Tips on making specific features testable are under the ["Creating tests" page](CreatingTests.md) but here are some tips for the whole Orchard Core app you want to test.

**Note** that certain features of the Lombiq UI Testing Toolbox need to be enabled from test code in addition to making the app testable. Check out `OrchardCoreUITestExecutorConfiguration`.

- Create recipes with test content, and import them by starting with a UI testing-specific setup recipe. While you can run tests from an existing database, using recipes to create a test environment (that almost entirely doubles as a development environment) is more reliable. Keep in mind, that the data you test shouldn't change randomly, you can't assert on data coming from the export of a production app which is updated all the time.
- If you make use of shortcuts then enable them in the test setup recipe:
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
- Tests should be self-contained and not rely on any external dependencies like APIs or CDNs. It should be possible to run the app as such. For static resources always have local copies available with CDN only being a possibility, and disable CDN usage in the setup recipe:
    ```json
    "steps": [
        {
            "name": "settings",
            "UseCdn": false
        }
    ]
    ```
- If you want to make use of any of the below features then in your web project do the following:
  1. Add a reference to `Lombiq.Tests.UI.SetupExtensions`.
  2. Allow configuration of the app when launched for testing with the following piece of code in the app's `Startup` class:
        ```csharp
        public void ConfigureServices(IServiceCollection services)
        {
            // _configuration is a constructor-injected IConfiguration instance.
            services.ConfigureUITesting(_configuration);
        }
        ``` 
- Some features send out e-mails. You can test them with the Lombiq UI Testing Toolbox's built-in feature to run an isolated local SMTP server with a web UI (see `OrchardCoreUITestExecutorConfiguration`). You need to enable the Email feature in the test setup recipe:
    ```json
    "steps": [
        {
            "name": "feature",
            "enable": [
                "OrchardCore.Email"
            ]
        }
    ]
    ``` 
    The rest of the configuration will be automatically applied.
- If you want the site to use Azure Blob Storage then you have to do the following:
  - Enable the OrchardCore.Media.Azure feature in your test setup recipe:
    ```json
    "steps": [
        {
            "name": "feature",
            "enable": [
                "OrchardCore.Media.Azure"
            ]
        }
    ]
    ```
  - It's also recommended that you use the [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator) as the storage of tests, not a real Azure Blob Storage resource. This is used by the UI Testing Toolbox by default. Be sure that it's running when the tests are executing.
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
