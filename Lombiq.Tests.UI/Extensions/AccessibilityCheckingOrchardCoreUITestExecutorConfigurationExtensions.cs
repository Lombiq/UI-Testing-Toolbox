using Deque.AxeCore.Commons;
using Deque.AxeCore.Selenium;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using Shouldly;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class AccessibilityCheckingOrchardCoreUITestExecutorConfigurationExtensions
{
    /// <summary>
    /// Sets up accessibility checking to run every time a page changes (either due to explicit navigation or
    /// clicks) and asserts on the validation results.
    /// </summary>
    /// <param name="assertAxeResult">
    /// The assertion logic to run on the result of an axe accessibility analysis. If <see langword="null"/> then the
    /// assertion supplied in the context will be used.
    /// </param>
    /// <param name="axeBuilderConfigurator">
    /// A delegate to configure the <see cref="AxeBuilder"/> instance. Will be applied after the configurator supplied
    /// in the context.
    /// </param>
    public static void SetUpAccessibilityCheckingAssertionOnPageChange(
        this OrchardCoreUITestExecutorConfiguration configuration,
        Action<AxeBuilder> axeBuilderConfigurator = null,
        Action<AccessibilityCheckingResult> assertAxeResult = null)
    {
        if (!configuration.CustomConfiguration.TryAdd("AccessibilityCheckingAssertionOnPageChangeWasSetUp", value: true)) return;

        configuration.Events.AfterPageChange += context =>
        {
            if (configuration.AccessibilityCheckingConfiguration.AccessibilityCheckingAndAssertionOnPageChangeRule?.Invoke(context) == true)
            {
                context.AssertAccessibility(axeBuilderConfigurator, assertAxeResult);
            }

            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// Shortcut for adding a filter to <see
    /// cref="OrchardCoreUITestExecutorConfiguration.AccessibilityCheckingConfiguration"/>'s <see
    /// cref="AccessibilityCheckingConfiguration.AxeResultIncompleteFilters"/>.
    /// </summary>
    public static void WithAxeIncompletesFilter(
        this OrchardCoreUITestExecutorConfiguration configuration,
        string name,
        Func<AxeResultItem, bool> filter) =>
        configuration.AccessibilityCheckingConfiguration.AxeResultIncompleteFilters[name] = filter;

    /// <summary>
    /// Shortcut for adding a filter to <see
    /// cref="OrchardCoreUITestExecutorConfiguration.AccessibilityCheckingConfiguration"/>'s <see
    /// cref="AccessibilityCheckingConfiguration.AxeResultIncompleteFilters"/>.
    /// </summary>
    public static void WithAxeIncompletesFilter(
        this OrchardCoreUITestExecutorConfiguration configuration,
        string name,
        string idToExclude) =>
        configuration.WithAxeIncompletesFilter(name, item => item.Id != idToExclude);

    /// <summary>
    /// Shortcut for adding a filter to <see
    /// cref="OrchardCoreUITestExecutorConfiguration.AccessibilityCheckingConfiguration"/>'s <see
    /// cref="AccessibilityCheckingConfiguration.AxeResultViolationsFilters"/>.
    /// </summary>
    public static void WithAxeViolationsFilters(
        this OrchardCoreUITestExecutorConfiguration configuration,
        string name,
        Func<AxeResultItem, bool> filter) =>
        configuration.AccessibilityCheckingConfiguration.AxeResultViolationsFilters.Add(name, filter);

    /// <summary>
    /// Shortcut for adding a filter to <see
    /// cref="OrchardCoreUITestExecutorConfiguration.AccessibilityCheckingConfiguration"/>'s <see
    /// cref="AccessibilityCheckingConfiguration.AxeResultViolationsFilters"/>.
    /// </summary>
    public static void WithAxeViolationsFilters(
        this OrchardCoreUITestExecutorConfiguration configuration,
        string name,
        string idToExclude) =>
        configuration.WithAxeViolationsFilters(name, item => item.Id != idToExclude);

    /// <summary>
    /// Adds exceptions for color contrast accessibility violations by selector.
    /// </summary>
    public static void WithAxeColorContrastViolationsFilters(
        this OrchardCoreUITestExecutorConfiguration configuration,
        params string[] selectors)
    {
        selectors.ShouldNotBeEmpty();
        configuration.WithAxeViolationsFilters(
            $"{nameof(WithAxeColorContrastViolationsFilters)}: \"{string.Join("\", \"", selectors)}\"",
            item => !(string.Equals(item.Id, "color-contrast", StringComparison.Ordinal) &&
                item.Nodes.TrueForAll(node => selectors.Exists(selector => node.Target.Selector.Contains(selector)))));
    }
}
