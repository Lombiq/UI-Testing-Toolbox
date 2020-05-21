using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Services;
using Selenium.Axe;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    public static class AccessibilityCheckingUITestContextExtensions
    {
        public static void AssertAccessibility(this UITestContext context)
        {
            var axeResult = context.AnalyzeAccessibility();

            try
            {
                context.Configuration.AccessibilityCheckingConfiguration.AssertAxeResult?.Invoke(axeResult);
            }
            catch (Exception ex)
            {
                throw new AccessibilityAssertionException(axeResult, ex);
            }
        }

        public static AxeResult AnalyzeAccessibility(this UITestContext context)
        {
            var axeBuilder = new AxeBuilder(context.Scope.Driver);
            context.Configuration.AccessibilityCheckingConfiguration.AxeBuilderConfigurator?.Invoke(axeBuilder);
            return axeBuilder.Analyze();
        }
    }
}
