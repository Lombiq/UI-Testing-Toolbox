using Atata;
using Atata.Cli;
using Lombiq.HelpfulLibraries.Common.Utilities;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

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

        // Since Atata 2.0 the default visibility option is Visibility.Any, these lines restore it to the 1.x behavior.
        AtataContext.GlobalConfiguration.UseDefaultControlVisibility(Visibility.Visible);
        SearchOptions.DefaultVisibility = Visibility.Visible;

        var timeoutConfiguration = configuration.TimeoutConfiguration;
        var browserConfiguration = configuration.BrowserConfiguration;

        var builder = AtataContext.Configure();

        if (browserConfiguration.Browser != Browser.None)
        {
            builder
                .UseDriverInitializationStage(AtataContextDriverInitializationStage.OnDemand)
                .UseDriver(await CreateDriverAsync(browserConfiguration, timeoutConfiguration, testOutputHelper));
        }
        else
        {
            builder.UseDriverInitializationStage(AtataContextDriverInitializationStage.None);
        }

        builder.UseBaseUrl(baseUri.ToString())
            .UseCulture(browserConfiguration.AcceptLanguage.ToString())
            .UseTestName(configuration.AtataConfiguration.TestName)
            .UseBaseRetryTimeout(timeoutConfiguration.RetryTimeout)
            .UseBaseRetryInterval(timeoutConfiguration.RetryInterval)
            .PageSnapshots.UseCdpOrPageSourceStrategy() // #spell-check-ignore-line
            .UseArtifactsPathTemplate(contextId); // Necessary to prevent long paths, an issue under Windows.

        builder.LogConsumers.AddDebug();
        builder.LogConsumers.Add(new TestOutputLogConsumer(testOutputHelper));

        configuration.AtataConfiguration.ContextBuilder?.Invoke(builder);

        return new AtataScope(builder.Build(), baseUri);
    }

    public static void SetupShellCliCommandFactory() =>
        ProgramCli.DefaultShellCliCommandFactory = OSDependentShellCliCommandFactory
            .UseCmdForWindows()
            .UseForOtherOS(new BashShellCliCommandFactory("-login"));

    private static async Task<IWebDriver> CreateDriverAsync(
        BrowserConfiguration browserConfiguration,
        TimeoutConfiguration timeoutConfiguration,
        ITestOutputHelper testOutputHelper)
    {
        Task<T> FromAsync<T>(Func<BrowserConfiguration, TimeSpan, Task<T>> factory)
            where T : IWebDriver =>
            factory(browserConfiguration, timeoutConfiguration.PageLoadTimeout);

        // Driver creation can fail with "Cannot start the driver service on http://localhost:56686/" exceptions if the
        // machine is under load. Retrying it here so not the whole test needs to be re-run.
        const int maxTryCount = 3;
        var currentTry = 1;

        // Force headless mode if we are in Linux without a working graphical environment.
        if (!browserConfiguration.Headless && OperatingSystem.IsLinux())
        {
            browserConfiguration.Headless = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY"));
        }

        while (true)
        {
            try
            {
                return browserConfiguration.Browser switch
                {
                    Browser.Chrome => await FromAsync(WebDriverFactory.CreateChromeDriverAsync),
                    Browser.Edge => await FromAsync(WebDriverFactory.CreateEdgeDriverAsync),
                    Browser.Firefox => await FromAsync(WebDriverFactory.CreateFirefoxDriverAsync),
                    Browser.InternetExplorer => await FromAsync(WebDriverFactory.CreateInternetExplorerDriverAsync),
                    Browser.None => null,
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
                    StringHelper.CreateInvariant($"{retryCount} more time(s). Exception: {ex}"));
            }
        }
    }
}
