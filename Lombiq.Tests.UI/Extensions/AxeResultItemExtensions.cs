using Lombiq.Tests.UI.Services;
using Selenium.Axe;
using Shouldly;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Extensions;

public static class AxeResultItemExtensions
{
    /// <summary>
    /// Check if <paramref name="axeResultItems"/> is empty, if not it converts the <see cref="AxeResultItem"/>s into
    /// human readable strings.
    /// </summary>
    public static void AxeResultItemsShouldBeEmpty(this IEnumerable<AxeResultItem> axeResultItems) =>
        axeResultItems.ShouldBeEmpty(AccessibilityCheckingConfiguration.AxeResultItemsToString(axeResultItems));
}
