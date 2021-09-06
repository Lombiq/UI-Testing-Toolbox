using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    public static class EventsOrchardCoreUITestExecutorConfigurationExtensions
    {
        public static void SetUpEvents(this OrchardCoreUITestExecutorConfiguration configuration)
        {
            if (!configuration.CustomConfiguration.TryAdd("EventsWereSetUp", true)) return;

            static bool IsNoAlert(UITestContext context)
            {
                // If there's an alert (which can happen mostly after a click but also after navigating) then all other
                // driver operations, even retrieving the current URL, will throw an UnhandledAlertException. Thus we
                // need to check if an alert is present and that's only possible by catching exceptions.
                try
                {
                    context.Driver.SwitchTo().Alert();
                    return false;
                }
                catch (NoAlertPresentException)
                {
                    return true;
                }
            }

            IWebElement html = null;

            configuration.Events.AfterNavigation += async (context, targetUri) =>
            {
                if (IsNoAlert(context)) await configuration.Events.AfterPageChange?.Invoke(context);
            };

            configuration.Events.BeforeClick += (context, targetElement) =>
            {
                html = context.Get(By.TagName("html"));
                return Task.CompletedTask;
            };

            configuration.Events.AfterClick += async (context, targetElement) =>
            {
                if (IsNoAlert(context))
                {
                    try
                    {
                        // A dummy access just to make Text throw an exception if the element is stale.
                        html.Text?.StartsWith("a", StringComparison.InvariantCulture);
                    }
                    catch (StaleElementReferenceException)
                    {
                        await configuration.Events.AfterPageChange?.Invoke(context);
                    }
                }
            };
        }
    }
}
