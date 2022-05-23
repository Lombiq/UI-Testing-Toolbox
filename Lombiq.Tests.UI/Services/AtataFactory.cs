using Atata;
using Atata.Cli;
using OpenQA.Selenium;
using System;
using System.Runtime.InteropServices;
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
    public static AtataScope StartAtataScope(
        ITestOutputHelper testOutputHelper,
        Uri baseUri,
        OrchardCoreUITestExecutorConfiguration configuration)
    {
        AtataContext.ModeOfCurrent = AtataContextModeOfCurrent.AsyncLocal;

        var timeoutConfiguration = configuration.TimeoutConfiguration;
        var browserConfiguration = configuration.BrowserConfiguration;

        var builder = AtataContext.Configure()
            // The drivers are disposed when disposing AtataScope.
#pragma warning disable CA2000 // Dispose objects before losing scope
            .UseDriver(CreateDriver(browserConfiguration, timeoutConfiguration, testOutputHelper))
#pragma warning restore CA2000 // Dispose objects before losing scope
            .UseBaseUrl(baseUri.ToString())
            .UseCulture(browserConfiguration.AcceptLanguage.ToString())
            .UseTestName(configuration.AtataConfiguration.TestName)
            .UseBaseRetryTimeout(timeoutConfiguration.RetryTimeout)
            .UseBaseRetryInterval(timeoutConfiguration.RetryInterval)
            .UseUtcTimeZone();

        builder.LogConsumers.AddDebug();
        builder.LogConsumers.Add(new TestOutputLogConsumer(testOutputHelper));

        configuration.AtataConfiguration.ContextBuilder?.Invoke(builder);

        return new AtataScope(builder.Build(), baseUri);
    }

    private static IWebDriver CreateDriver(
        BrowserConfiguration browserConfiguration,
        TimeoutConfiguration timeoutConfiguration,
        ITestOutputHelper testOutputHelper)
    {
        IWebDriver CastDriverFactory<T>(Func<BrowserConfiguration, TimeSpan, T> factory)
            where T : IWebDriver =>
            factory(browserConfiguration, timeoutConfiguration.PageLoadTimeout);

        // Driver creation can fail with "Cannot start the driver service on http://localhost:56686/" exceptions if the
        // machine is under load. Retrying it here so not the whole test needs to be re-run.
        const int maxTryCount = 3;
        var currentTryIndex = 0;

        // Force headless mode if we are in Linux without a working graphical environment.
        if (!browserConfiguration.Headless && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            browserConfiguration.Headless = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY"));
        }

        while (true)
        {
            try
            {
                return browserConfiguration.Browser switch
                {
                    Browser.Chrome => CastDriverFactory(WebDriverFactory.CreateChromeDriver),
                    Browser.Edge => CastDriverFactory(WebDriverFactory.CreateEdgeDriver),
                    Browser.Firefox => CastDriverFactory(WebDriverFactory.CreateFirefoxDriver),
                    Browser.InternetExplorer => CastDriverFactory(WebDriverFactory.CreateInternetExplorerDriver),
                    _ => throw new InvalidOperationException($"Unknown browser: {browserConfiguration.Browser}."),
                };
            }
            catch (WebDriverException ex)
            {
                if (ex.Message.ContainsOrdinalIgnoreCase("Cannot start the driver service on") &&
                    currentTryIndex < maxTryCount - 1)
                {
                    currentTryIndex++;
                    var retryCount = maxTryCount - currentTryIndex;

                    // Not using parameters because the exception can throw off the string format.
                    testOutputHelper.WriteLineTimestampedAndDebug(
                        "While creating the web driver failed with the following exception, it'll be retried " +
                        FormattableString.Invariant($"{retryCount} more time(s). Exception: {ex}"));
                }
                else
                {
                    throw;
                }
            }
        }
    }

    public static void SetupShellCliCommandFactory() =>
        ProgramCli.DefaultShellCliCommandFactory = OSDependentShellCliCommandFactory
            .UseCmdForWindows()
            .UseForOtherOS(new BashShellCliCommandFactory("-login"));
}
