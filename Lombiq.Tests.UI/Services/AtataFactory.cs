using Atata;
using Atata.Cli;
using Lombiq.HelpfulLibraries.Common.Utilities;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Services;

public class AtataConfiguration
{
    public string TestName { get; set; }
    public Action<AtataContextBuilder> ContextBuilder { get; set; }
}

public static class AtataFactory
{
    public static async Task<AtataScope> StartAtataScopeAsync(
        string contextId,
        ITestOutputHelper testOutputHelper,
        Uri baseUri,
        OrchardCoreUITestExecutorConfiguration configuration)
    {
        AtataContext.GlobalProperties.ModeOfCurrent = AtataContextModeOfCurrent.AsyncLocal;
        AtataContext.GlobalProperties.UseUtcTimeZone();

        SearchOptions.DefaultVisibility = Visibility.Visible;

        var timeoutConfiguration = configuration.TimeoutConfiguration;
        var browserConfiguration = configuration.BrowserConfiguration;

        var builder = AtataContext.CreateDefaultBuilder(AtataContextScope.Test);

        if (configuration.BrowserConfiguration.Browser != Browser.None)
        {
            var factory = await CreateDriverFactoryAsync(browserConfiguration, timeoutConfiguration, testOutputHelper);
            builder.Sessions.AddWebDriver(driverBuilder =>
            {
                driverBuilder.UseDriver(factory);
                driverBuilder.UseBaseUrl(baseUri);
                driverBuilder.PageSnapshots.UseCdpOrPageSourceStrategy();

                // Since Atata 2.0, the default visibility option is Visibility.Any. This restores it to the 1.x behavior.
                driverBuilder.UseDefaultControlVisibility(Visibility.Visible);
            });
        }

        builder.UseCulture(browserConfiguration.AcceptLanguage.ToString());
        builder.UseTestName(configuration.AtataConfiguration.TestName);
        builder.UseBaseRetryTimeout(timeoutConfiguration.RetryTimeout);
        builder.UseBaseRetryInterval(timeoutConfiguration.RetryInterval);

        // Necessary to prevent long paths, an issue under Windows.
        TestContext.Current.KeyValueStorage[nameof(ContextIdArtifactsPathFactory)] = contextId;
        AtataContext.GlobalProperties.ArtifactsPathFactory = ContextIdArtifactsPathFactory.Instance;

        builder.LogConsumers.AddDebug();
        builder.LogConsumers.Add(new TestOutputLogConsumer(testOutputHelper));

        configuration.AtataConfiguration.ContextBuilder?.Invoke(builder);

        var context = await builder.BuildAsync(TestContext.Current.CancellationToken);
        return new AtataScope(context, baseUri);
    }

    public static void SetupShellCliCommandFactory() =>
        ProgramCli.DefaultShellCliCommandFactory = OSDependentShellCliCommandFactory
            .UseCmdForWindows()
            .UseForOtherOS(new BashShellCliCommandFactory("-login"));

    private static async Task<Func<IWebDriver>> CreateDriverFactoryAsync(
        BrowserConfiguration browserConfiguration,
        TimeoutConfiguration timeoutConfiguration,
        ITestOutputHelper testOutputHelper)
    {
        // Driver creation can fail with "Cannot start the driver service on http://localhost:56686/" exceptions if the
        // machine is under load. Retrying it here so not the whole test needs to be re-run.
        const int maxTryCount = 3;
        var currentTry = 1;

        // Force headless mode if we are in Linux without a working graphical environment.
        if (!browserConfiguration.Headless && OperatingSystem.IsLinux())
        {
            browserConfiguration.Headless = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY"));
        }

        // Without this, Selenium Manager may use the installed browser if the major-minor versions are matching (e.g.
        // it may use the installed 134.0.6998.166 version if 134.0.6998.165 is requested). This is to enforce that the
        // exact version is used. See https://github.com/SeleniumHQ/selenium/issues/15517 for details.
        Environment.SetEnvironmentVariable("SE_FORCE_BROWSER_DOWNLOAD", "true");

        while (true)
        {
            try
            {
                var pageLoadTimeout = timeoutConfiguration.PageLoadTimeout;

                return browserConfiguration.Browser switch
                {
                    Browser.Chrome => await WebDriverFactory.CreateChromeDriverAsync(browserConfiguration, pageLoadTimeout),
                    Browser.Edge => await WebDriverFactory.CreateEdgeDriverAsync(browserConfiguration, pageLoadTimeout),
                    Browser.Firefox => await WebDriverFactory.CreateFirefoxDriverAsync(browserConfiguration, pageLoadTimeout),
                    _ => throw new InvalidOperationException($"Unknown browser: {browserConfiguration.Browser}."),
                };
            }
            catch (WebDriverException ex)
            {
                currentTry++;
                var retryCount = maxTryCount - currentTry;

                if (!ex.Message.ContainsOrdinalIgnoreCase("Cannot start the driver service on") || retryCount <= 0)
                {
                    throw;
                }

                // Not using parameters because the exception can throw off the string format.
                testOutputHelper.WriteLineTimestampedAndDebug(
                    "While creating the web driver failed with the following exception, it'll be retried " +
                    StringHelper.PluralizeInvariant("once more", "{0} more times", retryCount) + $". Exception: {ex}");
            }
        }
    }
}
