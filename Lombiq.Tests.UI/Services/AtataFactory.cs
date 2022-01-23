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
        public async static Task<AtataScope> StartAtataScopeAsync(
            ITestOutputHelper testOutputHelper,
            Uri baseUri,
            OrchardCoreUITestExecutorConfiguration configuration)
        {
            AtataContext.ModeOfCurrent = AtataContextModeOfCurrent.AsyncLocal;

            var timeoutConfiguration = configuration.TimeoutConfiguration;
            var browserConfiguration = configuration.BrowserConfiguration;

            RemoteWebDriver CreateDriver()
            {
                // Driver creation can fail with "Cannot start the driver service on http://localhost:56686/" exceptions
                // if the machine is under load. Retrying it here so not the whole test needs to be re-run.
                const int maxTryCount = 3;
                var i = 0;

                while (true)
                {
                    try
                    {
                        return browserConfiguration.Browser switch
                        {
                            Browser.Chrome => WebDriverFactory.CreateChromeDriver(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                            Browser.Edge => WebDriverFactory.CreateEdgeDriver(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                            Browser.Firefox => WebDriverFactory.CreateFirefoxDriver(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                            Browser.InternetExplorer =>
                                WebDriverFactory.CreateInternetExplorerDriver(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                            _ => throw new InvalidOperationException($"Unknown browser: {browserConfiguration.Browser}."),
                        };
                    }
                    catch (WebDriverException ex)
                    {
                        if (ex.Message.ContainsOrdinalIgnoreCase("Cannot start the driver service on") &&
                            i < maxTryCount - 1)
                        {
                            i++;
                            // Not using parameters because the exception can throw off the string format.
                            testOutputHelper.WriteLineTimestampedAndDebug(
                                $"While creating the web driver failed with the following exception, it'll be " +
                                $"retried {(maxTryCount - i).ToTechnicalString()} more time(s). Exception: {ex}");
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }

            var builder = AtataContext.Configure()
                // The drivers are disposed when disposing AtataScope.
#pragma warning disable CA2000 // Dispose objects before losing scope
                .UseDriver(await CreateDriver(browserConfiguration, timeoutConfiguration, testOutputHelper))
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

        private static async Task<RemoteWebDriver> CreateDriver(
            BrowserConfiguration browserConfiguration,
            TimeoutConfiguration timeoutConfiguration,
            ITestOutputHelper testOutputHelper)
        {
            // Driver creation can fail with "Cannot start the driver service on http://localhost:56686/" exceptions
            // if the machine is under load. Retrying it here so not the whole test needs to be re-run.
            const int maxTryCount = 3;
            var currentTryIndex = 0;

            while (true)
            {
                try
                {
                    return browserConfiguration.Browser switch
                    {
                        Browser.Chrome => await WebDriverFactory.CreateChromeDriverAsync(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                        Browser.Edge => await WebDriverFactory.CreateEdgeDriverAsync(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                        Browser.Firefox => await WebDriverFactory.CreateFirefoxDriverAsync(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                        Browser.InternetExplorer =>
                            await WebDriverFactory.CreateInternetExplorerDriverAsync(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                        _ => throw new InvalidOperationException($"Unknown browser: {browserConfiguration.Browser}."),
                    };
                }
                catch (WebDriverException ex)
                {
                    if (ex.Message.ContainsOrdinalIgnoreCase("Cannot start the driver service on") &&
                        currentTryIndex < maxTryCount - 1)
                    {
                        currentTryIndex++;
                        // Not using parameters because the exception can throw off the string format.
                        testOutputHelper.WriteLineTimestampedAndDebug(
                            $"While creating the web driver failed with the following exception, it'll be " +
                            $"retried {maxTryCount - currentTryIndex} more time(s). Exception: {ex}");
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
