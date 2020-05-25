using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Services;
using Selenium.Axe;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    public static class AccessibilityCheckingUITestContextExtensions
    {
        /// <summary>
        /// Executes assertions on the result of an axe accessibility analysis. Note that you need to run this after
        /// every page load, it won't accumulate during a session.
        /// </summary>
        /// <param name="assertAxeResult">
        /// The assertion logic to run on the result of an axe accessibility analysis. If <c>null</c> then the 
        /// assertion supplied in the context will be used.
        /// </param>
        /// <param name="axeBuilderConfigurator">
        /// A delegate to configure the <see cref="AxeBuilder"/> instance. Will be applied after the configurator 
        /// supplied in the context.
        /// </param>
        public static void AssertAccessibility(
            this UITestContext context,
            Action<AxeBuilder> axeBuilderConfigurator = null,
            Action<AxeResult> assertAxeResult = null)
        {
            var axeResult = context.AnalyzeAccessibility(axeBuilderConfigurator);

            try
            {
                if (assertAxeResult == null)
                {
                    context.Configuration.AccessibilityCheckingConfiguration.AssertAxeResult?.Invoke(axeResult);
                }
                else
                {
                    assertAxeResult?.Invoke(axeResult);
                }
            }
            catch (Exception ex)
            {
                throw new AccessibilityAssertionException(
                    axeResult, context.Configuration.AccessibilityCheckingConfiguration.CreateReportOnFailure, ex);
            }
        }

        /// <summary>
        /// Runs an axe accessibility analysis. Note that you need to run this after every page load, it won't 
        /// accumulate during a session.
        /// </summary>
        /// <param name="axeBuilderConfigurator">
        /// A delegate to configure the <see cref="AxeBuilder"/> instance. Will be applied after the configurator 
        /// supplied in the context.
        /// </param>
        /// <returns></returns>
        public static AxeResult AnalyzeAccessibility(this UITestContext context, Action<AxeBuilder> axeBuilderConfigurator = null)
        {
            var axeBuilder = new AxeBuilder(context.Scope.Driver);
            context.Configuration.AccessibilityCheckingConfiguration.AxeBuilderConfigurator?.Invoke(axeBuilder);
            axeBuilderConfigurator?.Invoke(axeBuilder);
            return axeBuilder.Analyze();
        }
    }
}
