using Atata;
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

            var builder = AtataContext.Configure()
                .UseDriver(browserConfiguration.Browser switch
                {
                    // The drivers are disposed when disposing the AtataScope.
#pragma warning disable CA2000 // Dispose objects before losing scope
                    Browser.Chrome => WebDriverFactory.CreateChromeDriver(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                    Browser.Edge => WebDriverFactory.CreateEdgeDriver(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                    Browser.Firefox => WebDriverFactory.CreateFirefoxDriver(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                    Browser.InternetExplorer => WebDriverFactory.CreateInternetExplorerDriver(browserConfiguration, timeoutConfiguration.PageLoadTimeout),
                    _ => throw new InvalidOperationException($"Unknown browser: {browserConfiguration.Browser}."),
#pragma warning restore CA2000 // Dispose objects before losing scope
                })
                .UseBaseUrl(baseUri.ToString())
                .UseCulture(browserConfiguration.AcceptLanguage.ToString())
                .UseTestName(configuration.AtataConfiguration.TestName)
                .AddDebugLogging()
                .AddLogConsumer(new TestOutputLogConsumer(testOutputHelper))
                .UseBaseRetryTimeout(timeoutConfiguration.RetryTimeout)
                .UseBaseRetryInterval(timeoutConfiguration.RetryInterval);

            configuration.AtataConfiguration.ContextBuilder?.Invoke(builder);

            return new AtataScope(builder.Build(), baseUri);
        }
    }
}
