using Lombiq.Tests.UI.Samples.Extensions;
using Lombiq.Tests.UI.Samples.Helpers;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples
{
    public class UITestBase : OrchardCoreUITestBase
    {
        protected override string AppAssemblyPath => WebAppConfig.GetAbsoluteApplicationAssemblyPath();

        protected UITestBase(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        protected Task ExecuteTestAfterSetupAsync(
            Action<UITestContext> test,
            Browser browser,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
            ExecuteTestAsync(test, browser, SetupHelpers.RunSetup, changeConfiguration);

        protected override Task ExecuteTestAsync(
            Action<UITestContext> test,
            Browser browser,
            Func<UITestContext, Uri> setupOperation = null,
            Action<OrchardCoreUITestExecutorConfiguration> changeConfiguration = null) =>
            base.ExecuteTestAsync(
                context =>
                {
                    context.SetStandardBrowserSize();

                    test(context);
                },
                browser,
                setupOperation,
                configuration =>
                {
                    configuration.BrowserConfiguration.Headless =
                        TestConfigurationManager.GetBoolConfiguration("BrowserConfiguration:Headless", true);

                    configuration.OrchardCoreConfiguration.BeforeAppStart +=
                        (contentRootPath, argumentsBuilder) =>
                        {
                            argumentsBuilder
                                .Add("--OrchardCore:Lombiq_Hosting_Azure_ApplicationInsights:EnableOfflineOperation")
                                .Add("true");
                            return Task.CompletedTask;
                        };

                    configuration.HtmlValidationConfiguration.RunHtmlValidationAssertionOnAllPageChanges = true;
                    configuration.HtmlValidationConfiguration.AssertHtmlValidationResult =
                        AssertHtmlValidationResultHelpers.DefaultAssertHtmlValidationOutput;

                    changeConfiguration?.Invoke(configuration);
                });
    }
}
