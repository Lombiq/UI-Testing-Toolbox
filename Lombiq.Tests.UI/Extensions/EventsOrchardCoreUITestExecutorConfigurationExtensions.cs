using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Models;
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
            if (!configuration.CustomConfiguration.TryAdd("EventsWereSetUp", value: true)) return;

            PageNavigationState navigationState = null;

            configuration.Events.AfterNavigation += (context, _) => OnEventsAfterNavigationAsync(context);

            configuration.Events.BeforeClick += (context, _) =>
            {
                navigationState = context.AsPageNavigationState();
                return Task.CompletedTask;
            };

            configuration.Events.AfterClick += (context, _) =>
                navigationState.CheckIfNavigationHasOccurred()
                    ? OnEventsAfterNavigationAsync(context)
                    : Task.CompletedTask;
        }

        private static bool IsNoAlert(UITestContext context)
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

        private static async Task OnEventsAfterNavigationAsync(UITestContext context)
        {
            if (IsNoAlert(context) &&
                context.Configuration.Events.AfterPageChange is { } afterPageChange)
            {
                try
                {
                    await afterPageChange.Invoke(context);
                }
                catch (Exception exception)
                {
                    throw new PageChangeAssertionException(context, exception);
                }
            }
        }
    }
}
