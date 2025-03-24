using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Extensions;

public static class NavigationWebElementExtensions
{
    /// <summary>
    /// Clicks an element even if the default Click() will sometimes fail to do so. It's more reliable than Click() but
    /// still not perfect. If you're doing a Get() before then use <see
    /// cref="NavigationUITestContextExtensions.ClickReliablyOnAsync(UITestContext, By, int)"/> instead.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Even when the element is absolutely, positively there (Atata's Get() succeeds) the clicks sometimes simply don't
    /// go through the first time. More literature on the scientific field of clicking (but the code there doesn't
    /// really help): https://cezarypiatek.github.io/post/why-click-with-selenium-so-hard/ Also see:
    /// https://stackoverflow.com/questions/11908249/debugging-element-is-not-clickable-at-point-error.
    /// </para>
    /// </remarks>
    /// <param name="maxTries">The maximum number of clicks attempted altogether, if retries are needed.</param>
    public static Task ClickReliablyAsync(this IWebElement element, UITestContext context, int maxTries = 3) =>
        context.ExecuteLoggedAsync(
            nameof(ClickReliablyAsync),
            element,
            async () =>
            {
                await context.Configuration.Events.BeforeClick
                    .InvokeAsync<ClickEventHandler>(eventHandler => eventHandler(context, element));

                // When the button is under some overhanging UI element, the MoveToElement sometimes fails with the
                // "move target out of bounds" exception message. And while the UI is changing, wrongly timed clicks can
                // fail with StaleElementReferenceExceptions. In these cases it should be retried.
                var notFound = true;
                for (var i = 1; notFound && i <= maxTries; i++)
                {
                    try
                    {
                        context.Driver.Perform(actions => actions.MoveToElement(element).Click());
                        notFound = false;
                    }
                    catch (WebDriverException ex) when (i < maxTries)
                    {
                        switch (ex.Message)
                        {
                            case string message when message.Contains("move target out of bounds"):
                                context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug(
                                    "\"move target out of bounds\" exception, retrying the click.");
                                break;

                            case string message when ex.IsStateElementLikeException():
                                context.Configuration.TestOutputHelper.WriteLineTimestampedAndDebug(
                                    "Stale element exception with the message \"{0}\", retrying the click.",
                                    message);
                                break;

                            case string message when message.ContainsOrdinalIgnoreCase(
                                "javascript error: Failed to execute 'elementsFromPoint' on 'Document': The provided double value is non-finite."):
                                throw new NotSupportedException(
                                    "For this element use the standard Click() method.");

                            default:
                                throw;
                        }

                        await Task.Delay(RetrySettings.Interval, context.Configuration.TestCancellationToken);
                    }
                }

                await context.Configuration.Events.AfterClick
                    .InvokeAsync<ClickEventHandler>(eventHandler => eventHandler(context, element));
            });

    /// <inheritdoc cref="ClickReliablyUntilNavigationHasOccurredAsync(IWebElement, UITestContext, TimeSpan?, TimeSpan?)"/>
    [Obsolete("Use ClickReliablyUntilNavigationHasOccurredAsync instead.")]
    public static Task ClickReliablyUntilPageLeaveAsync(
        this IWebElement element,
        UITestContext context,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        element.ClickReliablyUntilNavigationHasOccurredAsync(context, timeout, interval);

    /// <summary>
    /// Repeatedly clicks an element until the browser leaves the page. Note that unlike <see
    /// cref="ClickReliablyUntilUrlChangeAsync"/> this doesn't just necessitate a URL change but also a page leave. If
    /// you're doing a Get() before then use <see
    /// cref="NavigationUITestContextExtensions.ClickReliablyOnUntilPageLeaveAsync(UITestContext, By, TimeSpan?,
    /// TimeSpan?)"/> instead.
    /// </summary>
    public static Task ClickReliablyUntilNavigationHasOccurredAsync(
        this IWebElement element,
        UITestContext context,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        context.DoWithRetriesUntilNavigationHasOccurredOrFailAsync(
            () => element.ClickReliablyAsync(context),
            timeout,
            interval);

    /// <summary>
    /// Repeatedly clicks an element until the browser URL changes. Note that unlike <see
    /// cref="ClickReliablyUntilNavigationHasOccurredAsync"/> this doesn't necessitate a navigation, but can include it.
    /// If you're doing a Get() before then use <see
    /// cref="NavigationUITestContextExtensions.ClickReliablyOnUntilUrlChangeAsync(UITestContext, By, TimeSpan?,
    /// TimeSpan?)"/> instead.
    /// </summary>
    public static Task ClickReliablyUntilUrlChangeAsync(
        this IWebElement element,
        UITestContext context,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        context.DoWithRetriesUntilUrlChangeOrFailAsync(
            () => element.ClickReliablyAsync(context),
            timeout,
            interval);

    /// <summary>
    /// Clicks the given element with a JavaScript <c>click()</c>. This can work when <see
    /// cref="ClickReliablyAsync(IWebElement, UITestContext, int)"/> fails even with retries.
    /// </summary>
    public static Task ClickWithScriptAsync(this IWebElement element, UITestContext context) =>
        context.ExecuteLoggedAsync(
            nameof(ClickWithScriptAsync),
            element,
            async () =>
            {
                await context.Configuration.Events.BeforeClick
                    .InvokeAsync<ClickEventHandler>(eventHandler => eventHandler(context, element));

                context.ExecuteScript("arguments[0].click();", element);

                await context.Configuration.Events.AfterClick
                    .InvokeAsync<ClickEventHandler>(eventHandler => eventHandler(context, element));
            });
}
