using Atata;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;

namespace Lombiq.Tests.UI.Attributes.Behaviors;

public sealed class SetsValueReliablyAttribute : ValueSetBehaviorAttribute
{
    public override void Execute<TOwner>(IUIComponent<TOwner> component, string value) // #spell-check-ignore-line
    {
        var element = component.Scope;
        var driver = component.Context.Driver;

        ReliabilityHelper.DoWithRetriesOrFail(
            () => driver.TryFillElement(element, value).GetValue() == value);
    }
}
