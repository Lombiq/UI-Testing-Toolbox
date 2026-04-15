using Deque.AxeCore.Commons;
using Deque.AxeCore.Selenium;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Services;

public class AccessibilityCheckingConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to create an accessibility report if the given test fails accessibility
    /// checking.
    /// </summary>
    public bool CreateReportOnFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to create an accessibility report for every test, regardless of them
    /// failing or not. You can use this to e.g. compile an accessibility report for the whole app, encompassing all
    /// pages checked by tests. The reports will be added to the test dump.
    /// </summary>
    public bool CreateReportAlways { get; set; }

    /// <summary>
    /// Gets or sets a configuration delegate for the <see cref="AxeBuilder"/> instance used for accessibility checking.
    /// For more information on the various options see <see
    /// href="https://troywalshprof.github.io/SeleniumAxeDotnet/#/?id=axebuilder-reference"/>. Defaults to <see
    /// cref="ConfigureWcag22aa"/>.
    /// </summary>
    public Action<AxeBuilder> AxeBuilderConfigurator { get; set; } = axeBuilder => ConfigureWcag22aa(axeBuilder);

    /// <summary>
    /// Gets or sets a value indicating whether to automatically run accessibility checks every time a page changes
    /// (either due to explicit navigation or clicks) and assert on the validation results.
    /// </summary>
    public bool RunAccessibilityCheckingAssertionOnAllPageChanges { get; set; }

    /// <summary>
    /// Gets or sets a predicate that determines whether accessibility checking and asserting the results should run for
    /// the current page. This is only used if <see cref="RunAccessibilityCheckingAssertionOnAllPageChanges"/> is set to
    /// <see langword="true"/>. Defaults to <see
    /// cref="EnableOnValidatablePagesAccessibilityCheckingAndAssertionOnPageChangeRule"/>.
    /// </summary>
    public Predicate<UITestContext> AccessibilityCheckingAndAssertionOnPageChangeRule { get; set; } =
        EnableOnValidatablePagesAccessibilityCheckingAndAssertionOnPageChangeRule;

    /// <summary>
    /// Gets a collection of delegates that select which <see cref="AccessibilityCheckingResult.Incomplete"/> is
    /// retained. If there are more than one filters, all of them must return <see langword="true"/>.
    /// </summary>
    public IDictionary<string, Func<AxeResultItem, bool>> AxeResultIncompleteFilters { get; } =
        new Dictionary<string, Func<AxeResultItem, bool>>();

    /// <summary>
    /// Gets a collection of delegates that select which <see cref="AccessibilityCheckingResult.Violations"/> is
    /// retained. If there are more than one filters, all of them must return <see langword="true"/>.
    /// </summary>
    public IDictionary<string, Func<AxeResultItem, bool>> AxeResultViolationsFilters { get; } =
        new Dictionary<string, Func<AxeResultItem, bool>>();

    /// <summary>
    /// Gets or sets a delegate to run assertions on the <see cref="AccessibilityCheckingResult"/> when accessibility
    /// checking happens. Defaults to <see cref="AssertAxeResultIsEmpty"/>.
    /// </summary>
    public Action<AccessibilityCheckingResult> AssertAxeResult { get; set; } = AssertAxeResultIsEmpty;

    /// <summary>
    /// Configures the given <see cref="AxeBuilder"/> to check for WCAG 2.1 AA compliance. Use the newer
    /// <see cref="ConfigureWcag22aa"/> instead if you want to check for WCAG 2.2 AA compliance, which includes all the
    /// WCAG 2.1 AA rules as well.
    /// </summary>
    public static readonly Func<AxeBuilder, AxeBuilder> ConfigureWcag21aa = axeBuilder =>
        axeBuilder.WithTags("wcag2a", "wcag2aa", "wcag21a", "wcag21aa");

    /// <summary>
    /// Configures the given <see cref="AxeBuilder"/> to check for WCAG 2.2 AA compliance, which includes all the WCAG
    /// 2.1 AA rules as well. This is a good default to use, as it checks for the latest WCAG version's AA compliance.
    /// Consider going one step further and use <see cref="ConfigureWcag22aaWithBestPractices"/> to also include
    /// industry accepted best practices that improve the user experience.
    /// </summary>
    public static readonly Func<AxeBuilder, AxeBuilder> ConfigureWcag22aa = axeBuilder =>
        axeBuilder.WithTags("wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22a", "wcag22aa");

    /// <summary>
    /// Configures the given <see cref="AxeBuilder"/> to check for WCAG 2.2 AA compliance, as well as axe Best Practices
    /// Rules. This is a good default to use, as it checks for the latest WCAG version's AA compliance and also includes
    /// industry accepted best practices that improve the user experience.
    /// </summary>
    public static readonly Func<AxeBuilder, AxeBuilder> ConfigureWcag22aaWithBestPractices = axeBuilder =>
        axeBuilder.WithTags("wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22a", "wcag22aa", "best-practice");

    public static readonly Action<AccessibilityCheckingResult> AssertAxeResultIsEmpty = axeResult =>
    {
        axeResult.Violations.AxeResultItemsShouldBeEmpty();
        axeResult.Incomplete.AxeResultItemsShouldBeEmpty();
    };

    public static readonly Func<IEnumerable<AxeResultItem>, string> AxeResultItemsToString =
        items =>
            string.Join(
                Environment.NewLine,
                items.Select(item =>
                    $"{item.Help}: {Environment.NewLine}{string.Join(Environment.NewLine, item.Nodes.Select(node => "    " + node.Html))}"));

    public static readonly Predicate<UITestContext> EnableOnValidatablePagesAccessibilityCheckingAndAssertionOnPageChangeRule =
        UrlCheckHelper.IsValidatablePage;
}
