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

            var builder = AtataContext.Configure()
                .UseDriver(configuration.Browser switch
                {
                    Browser.Chrome => WebDriverFactory.CreateChromeDriver(timeoutConfiguration.PageLoadTimeout),
                    Browser.Edge => WebDriverFactory.CreateEdgeDriver(timeoutConfiguration.PageLoadTimeout),
                    Browser.Firefox => WebDriverFactory.CreateFirefoxDriver(timeoutConfiguration.PageLoadTimeout),
                    Browser.InternetExplorer => WebDriverFactory.CreateInternetExplorerDriver(timeoutConfiguration.PageLoadTimeout),
                    _ => throw new InvalidOperationException($"Unknown browser: {configuration.Browser}.")
                })
                .UseBaseUrl(baseUri.ToString())
                .UseCulture("en-us")
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
