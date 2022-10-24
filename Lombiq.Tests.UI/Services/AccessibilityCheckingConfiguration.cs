using Lombiq.Tests.UI.Helpers;
using Selenium.Axe;
using Shouldly;
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
    /// Gets or sets a value indicating whether to create an accessibility report for every test. You can use this to
    /// e.g. compile an accessibility report for the whole app, encompassing all pages checked by tests.
    /// </summary>
    public bool CreateReportAlways { get; set; }

    /// <summary>
    /// Gets or sets the (relative or absolute) path where those accessibility reports are stored that are created for
    /// every test (see <see cref="CreateReportAlways"/>).
    /// </summary>
    public string AlwaysCreatedAccessibilityReportsDirectoryPath { get; set; } = "AccessibilityReports";

    /// <summary>
    /// Gets or sets a configuration delegate for the <see cref="AxeBuilder"/> instance used for accessibility checking.
    /// For more information on the various options see <see
    /// href="https://troywalshprof.github.io/SeleniumAxeDotnet/#/?id=axebuilder-reference"/>. Defaults to <see
    /// cref="ConfigureWcag21aa"/>.
    /// </summary>
    public Action<AxeBuilder> AxeBuilderConfigurator { get; set; } = axeBuilder => ConfigureWcag21aa(axeBuilder);

    /// <summary>
    /// Gets or sets a value indicating whether to automatically run accessibility checks every time a page changes
    /// (either due to explicit navigation or clicks) and assert on the validation results.
    /// </summary>
    public bool RunAccessibilityCheckingAssertionOnAllPageChanges { get; set; }

    // Once this method is removed, remove 'Accessbility' from the spelling whitelist in GitHub Actions.
    [Obsolete("Use the correctly named AccessibilityCheckingAndAssertionOnPageChangeRule instead.")]
    public Predicate<UITestContext> AccessbilityCheckingAndAssertionOnPageChangeRule
    {
        get => AccessibilityCheckingAndAssertionOnPageChangeRule;
        set => AccessibilityCheckingAndAssertionOnPageChangeRule = value;
    }

    /// <summary>
    /// Gets or sets a predicate that determines whether accessibility checking and asserting the results should run for
    /// the current page. This is only used if <see cref="RunAccessibilityCheckingAssertionOnAllPageChanges"/> is set to
    /// <see langword="true"/>. Defaults to <see
    /// cref="EnableOnValidatablePagesAccessibilityCheckingAndAssertionOnPageChangeRule"/>.
    /// </summary>
    public Predicate<UITestContext> AccessibilityCheckingAndAssertionOnPageChangeRule { get; set; } =
        EnableOnValidatablePagesAccessibilityCheckingAndAssertionOnPageChangeRule;

    /// <summary>
    /// Gets or sets a delegate to run assertions on the <see cref="AxeResult"/> when accessibility checking happens.
    /// Defaults to <see cref="AssertAxeResultIsEmpty"/>.
    /// </summary>
    public Action<AxeResult> AssertAxeResult { get; set; } = AssertAxeResultIsEmpty;

    // Returns AxeBuilder so it can be chained.
    public static readonly Func<AxeBuilder, AxeBuilder> ConfigureWcag21aa = axeBuilder =>
        axeBuilder.WithTags("wcag2a", "wcag2aa", "wcag21a", "wcag21aa");

    public static readonly Action<AxeResult> AssertAxeResultIsEmpty = axeResult =>
    {
        axeResult.Violations.ShouldBeEmpty(AxeResultItemsToString(axeResult.Violations));
        axeResult.Incomplete.ShouldBeEmpty(AxeResultItemsToString(axeResult.Incomplete));
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
