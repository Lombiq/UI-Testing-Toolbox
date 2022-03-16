using Lombiq.Tests.UI.Services;
using Selenium.Axe;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class AccessibilityCheckingOrchardCoreUITestExecutorConfigurationExtensions
{
    /// <summary>
    /// Sets up accessibility checking to run every time a page changes (either due to explicit navigation or
    /// clicks) and asserts on the validation results.
    /// </summary>
    /// <param name="assertAxeResult">
    /// The assertion logic to run on the result of an axe accessibility analysis. If <see langword="null"/> then
    /// the assertion supplied in the context will be used.
    /// </param>
    /// <param name="axeBuilderConfigurator">
    /// A delegate to configure the <see cref="AxeBuilder"/> instance. Will be applied after the configurator
    /// supplied in the context.
    /// </param>
    public static void SetUpAccessibilityCheckingAssertionOnPageChange(
        this OrchardCoreUITestExecutorConfiguration configuration,
        Action<AxeBuilder> axeBuilderConfigurator = null,
        Action<AxeResult> assertAxeResult = null)
    {
        if (!configuration.CustomConfiguration.TryAdd("AccessibilityCheckingAssertionOnPageChangeWasSetUp", value: true)) return;

        configuration.Events.AfterPageChange += context =>
        {
            if (configuration.AccessibilityCheckingConfiguration.AccessbilityCheckingAndAssertionOnPageChangeRule?.Invoke(context) == true)
            {
                context.AssertAccessibility(axeBuilderConfigurator, assertAxeResult);
            }

            return Task.CompletedTask;
        };
    }
}
