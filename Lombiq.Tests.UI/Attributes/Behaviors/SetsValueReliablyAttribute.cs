using Atata;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using System;
using System.Threading;

namespace Lombiq.Tests.UI.Attributes.Behaviors;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SetsValueReliablyAttribute : ValueSetBehaviorAttribute
{
    public override void Execute<TOwner>(IUIComponent<TOwner> component, string value) // #spell-check-ignore-line
    {
        var element = component.Scope;
        var driver = component.Context.Driver;

        ReliabilityHelper.DoWithRetriesOrFail(
            () => driver.TryFillElement(element, value).GetValue() == value,
            cancellationToken: CancellationToken.None);
    }
}
