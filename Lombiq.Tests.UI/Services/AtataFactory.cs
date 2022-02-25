using Atata;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Services
{
    public class AtataConfiguration
    {
        public string TestName { get; set; }
        public Action<AtataContextBuilder> ContextBuilder { get; set; }
    }

    public static class AtataFactory
    {
        public static async Task<AtataScope> StartAtataScopeAsync(
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
                .UseDriver(await CreateDriverAsync(browserConfiguration, timeoutConfiguration, testOutputHelper))
#pragma warning restore CA2000 // Dispose objects before losing scope
                .UseBaseUrl(baseUri.ToString())
                .UseCulture(browserConfiguration.AcceptLanguage.ToString())
                .UseTestName(configuration.AtataConfiguration.TestName)
                .AddDebugLogging()
                .AddLogConsumer(new TestOutputLogConsumer(testOutputHelper))
                .UseBaseRetryTimeout(timeoutConfiguration.RetryTimeout)
                .UseBaseRetryInterval(timeoutConfiguration.RetryInterval)
                .UseUtcTimeZone();

            configuration.AtataConfiguration.ContextBuilder?.Invoke(builder);

            return new AtataScope(builder.Build(), baseUri);
        }

        private static async Task<RemoteWebDriver> CreateDriverAsync(
            BrowserConfiguration browserConfiguration,
            TimeoutConfiguration timeoutConfiguration,
            ITestOutputHelper testOutputHelper)
        {
            async Task<RemoteWebDriver> CastDriverFactoryAsync<T>(Func<BrowserConfiguration, TimeSpan, Task<T>> factory)
                where T : RemoteWebDriver =>
                await factory(browserConfiguration, timeoutConfiguration.PageLoadTimeout);

            // Driver creation can fail with "Cannot start the driver service on http://localhost:56686/" exceptions
            // if the machine is under load. Retrying it here so not the whole test needs to be re-run.
            const int maxTryCount = 3;
            var currentTryIndex = 0;

            while (true)
            {
                try
                {
                    var task = browserConfiguration.Browser switch
                    {
                        Browser.Chrome => CastDriverFactoryAsync(WebDriverFactory.CreateChromeDriverAsync),
                        Browser.Edge => CastDriverFactoryAsync(WebDriverFactory.CreateEdgeDriverAsync),
                        Browser.Firefox => CastDriverFactoryAsync(WebDriverFactory.CreateFirefoxDriverAsync),
                        Browser.InternetExplorer => CastDriverFactoryAsync(WebDriverFactory.CreateInternetExplorerDriverAsync),
                        _ => throw new InvalidOperationException($"Unknown browser: {browserConfiguration.Browser}."),
                    };
                    return await task;
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
    }
}
