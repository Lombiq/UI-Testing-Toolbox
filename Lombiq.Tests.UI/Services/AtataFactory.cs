using Atata;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using System;
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
        public static AtataScope StartAtataScope(
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
                            Browser.InternetExplorer => WebDriverFactory.CreateInternetExplorerDriver(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                            _ => throw new InvalidOperationException($"Unknown browser: {browserConfiguration.Browser}."),
                        };
                    }
                    catch (WebDriverException ex)
                    {
                        if (ex.Message.Contains("Cannot start the driver service on", StringComparison.InvariantCulture) &&
                            i < maxTryCount - 1)
                        {
                            i++;
                            // Not using parameters because the exception can throw off the string format.
                            testOutputHelper.WriteLineTimestampedAndDebug(
                                $"While creating the web driver failed with the following exception, it'll be retried {maxTryCount - i} more time(s). Exception: {ex}");
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
                .UseDriver(CreateDriver())
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
    }
}
