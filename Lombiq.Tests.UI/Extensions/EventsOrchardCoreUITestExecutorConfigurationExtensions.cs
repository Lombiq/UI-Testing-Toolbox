using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions
{
    public static class EventsOrchardCoreUITestExecutorConfigurationExtensions
    {
        public static void SetUpEvents(this OrchardCoreUITestExecutorConfiguration configuration)
        {
            if (!configuration.CustomConfiguration.TryAdd("EventsWereSetUp", value: true)) return;

            PageNavigationState navigationState = null;

            configuration.Events.AfterNavigation += (context, _) => context.TriggerAfterPageChangeEventAsync();

            configuration.Events.BeforeClick += (context, _) =>
            {
                navigationState = context.AsPageNavigationState();
                return Task.CompletedTask;
            };

            configuration.Events.AfterClick += (context, _) =>
                navigationState.CheckIfNavigationHasOccurred()
                    ? context.TriggerAfterPageChangeEventAsync()
                    : Task.CompletedTask;
        }
    }
}
