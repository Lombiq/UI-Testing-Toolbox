using Lombiq.Tests.UI.Services;
using Selenium.Axe;
using Shouldly;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Extensions;

public static class AxeResultItemExtensions
{
    /// <summary>
    /// Asserts if <paramref name="axeResultItems"/> is empty, and if not then produces an error with <see
    /// cref="AxeResultItem"/>s converted into human-readable strings.
    /// </summary>
    public static void AxeResultItemsShouldBeEmpty(this IEnumerable<AxeResultItem> axeResultItems) =>
        axeResultItems.ShouldBeEmpty(AccessibilityCheckingConfiguration.AxeResultItemsToString(axeResultItems));
}
