using Atata;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using System;

namespace Lombiq.Tests.UI.Attributes.Behaviors
{
    public sealed class SetsValueReliablyAttribute : ValueSetBehaviorAttribute
    {
        public override void Execute<TOwner>(IUIComponent<TOwner> component, string value)
        {
            var element = component.Scope;
            var driver = component.Context.Driver;

            ReliabilityHelper.DoWithRetriesOrFail(
                () => driver.TryFillElement(element, value).GetValue().EqualsOrdinal(value));
        }
    }
}
