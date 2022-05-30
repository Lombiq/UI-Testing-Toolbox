using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class NavigationWebElementExtensions
{
    /// <summary>
    /// Clicks an element even if the default Click() will sometimes fail to do so. It's more reliable than Click() but
    /// still not perfect. If you're doing a Get() before then use <see
    /// cref="NavigationUITestContextExtensions.ClickReliablyOnAsync(UITestContext, By)"/> instead.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Even when the element is absolutely, positively there (Atata's Get() succeeds) the clicks sometimes simply don't
    /// go through the first time. More literature on the scientific field of clicking (but the code there doesn't
    /// really help): https://cezarypiatek.github.io/post/why-click-with-selenium-so-hard/ Also see:
    /// https://stackoverflow.com/questions/11908249/debugging-element-is-not-clickable-at-point-error.
    /// </para>
    /// </remarks>
    public static Task ClickReliablyAsync(this IWebElement element, UITestContext context) =>
        context.ExecuteLoggedAsync(
            nameof(ClickReliablyAsync),
            element,
            async () =>
            {
                try
                {
                    await context.Configuration.Events.BeforeClick
                        .InvokeAsync<ClickEventHandler>(eventHandler => eventHandler(context, element));

                    // When the button is under some overhanging UI element, the MoveToElement sometimes fails with the
                    // "move target out of bounds" exception message. In this case it should be retried.
                    const int maxTries = 3;
                    var notFound = true;
                    for (var i = 1; notFound && i <= maxTries; i++)
                    {
                        try
                        {
                            context.Driver.Perform(actions => actions.MoveToElement(element).Click());
                            notFound = false;
                        }
                        catch (WebDriverException ex) when (i < maxTries && ex.Message.Contains("move target out of bounds"))
                        {
                            await Task.Delay(RetrySettings.Interval);
                        }
                    }

                    await context.Configuration.Events.AfterClick
                        .InvokeAsync<ClickEventHandler>(eventHandler => eventHandler(context, element));
                }
                catch (WebDriverException ex)
                    when (ex.Message.ContainsOrdinalIgnoreCase(
                        "javascript error: Failed to execute 'elementsFromPoint' on 'Document': The provided double value is non-finite."))
                {
                    throw new NotSupportedException(
                        "For this element use the standard Click() method.");
                }
            });

    /// <summary>
    /// Repeatedly clicks an element until the browser leaves the page. If you're doing a Get() before then use <see
    /// cref="NavigationUITestContextExtensions.ClickReliablyOnAsync(UITestContext, By)"/> instead.
    /// </summary>
    public static void ClickReliablyUntilPageLeave(
        this IWebElement element,
        UITestContext context,
        TimeSpan? timeout = null,
        TimeSpan? interval = null) =>
        context.RetryIfNotStaleOrFailAsync(
            async () =>
            {
                await element.ClickReliablyAsync(context);
                return false;
            },
            timeout,
            interval);
}
