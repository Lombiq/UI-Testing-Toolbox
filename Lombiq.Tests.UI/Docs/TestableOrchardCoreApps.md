# Making an Orchard Core app testable



Tips on making specific features testable are under the ["Creating tests" page](CreatingTests.md) but here are some tips for the whole Orchard Core app you want to test.

- Create recipes with test content, and import them by starting with a UI testing-specific setup recipe. While you can run tests from an existing database, using recipes to create a test environment (that almost entirely doubles as a development environment) is more reliable. Keep in mind, that the data you test shouldn't change randomly, you can't assert on data coming from the export of a production app which is updated all the time.
- Tests should be self-contained and not rely on any external dependencies like APIs or CDNs. It should be possible to run the app as such. For static resources always have local copies available with CDN only being a possibility, and disable CDN usage in the setup recipe:
    ```json
    "steps": [
      {
        "name": "settings",
        "UseCdn": false
      }
    ]
    ```
- If features send out e-mails that you want to test too then you can use the Lombiq UI Testing Toolbox's built-in feature to run an isolated local SMTP server with a web UI (see `OrchardCoreUITestExecutorConfiguration`). For this to work, you need to do two things:
    - Configure the basic SMTP settings from the mentioned test setup recipe:
        ```json
        "steps": [
            {
                "SmtpSettings": {
                    "DefaultSender": "email@example.com",
                    "Host": "127.0.0.1"
                }
            }
        ]
        ```
    - Since the SMTP port needs to be configured on the fly when the app is launched for test execution you also need to override the port from the `SmtpPort` configuration parameter in the app's `Startup` class:
        ```csharp
        public void ConfigureServices(IServiceCollection services)
        {
            services.PostConfigure<SmtpSettings>(settings =>
            {
                var port = _configuration.GetValue<int>("SmtpPort");
                if (port != 0) settings.Port = port;
            });
        }

        ```
