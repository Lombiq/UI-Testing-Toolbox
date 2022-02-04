using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    public static class NavigationWebElementExtensions
    {
        /// <summary>
        /// Clicks an element even if the default Click() will sometimes fail to do so. It's more reliable than Click()
        /// but still not perfect. If you're doing a Get() before then use <see
        /// cref="NavigationUITestContextExtensions.ClickReliablyOn(UITestContext, By)"/> instead.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Even when the element is absolutely, positively there (Atata's Get() succeeds) the clicks sometimes simply
        /// don't go through the first time. More literature on the scientific field of clicking (but the code there
        /// doesn't really help): https://cezarypiatek.github.io/post/why-click-with-selenium-so-hard/ Also see:
        /// https://stackoverflow.com/questions/11908249/debugging-element-is-not-clickable-at-point-error.
        /// </para>
        /// </remarks>
        public static void ClickReliably(this IWebElement element, UITestContext context) =>
            context.ExecuteLogged(
                nameof(ClickReliably),
                element,
                () =>
                {
                    try
                    {
                        context.Configuration.Events.BeforeClick
                            .InvokeAsync<ClickEventHandler>(eventHandler => eventHandler(context, element))
                            .GetAwaiter()
                            .GetResult();

                        context.Driver.Perform(actions => actions.MoveToElement(element).Click());
                        context.Configuration.Events.AfterClick
                            .InvokeAsync<ClickEventHandler>(eventHandler => eventHandler(context, element))
                            .GetAwaiter()
                            .GetResult();
                    }
                    catch (WebDriverException ex)
                        when (ex.Message.ContainsOrdinalIgnoreCase(
                            "javascript error: Failed to execute 'elementsFromPoint' on 'Document': The provided double value is non-finite."))
                    {
                        throw new NotSupportedException(
                            "For this element use the standard Click() method. Add the element as an exception to the documentation.");
                    }
                });

        /// <summary>
        /// Repeatedly clicks an element until the browser leaves the page. If you're doing a Get() before then use
        /// <see cref="NavigationUITestContextExtensions.ClickReliablyOn(UITestContext, By)"/> instead.
        /// </summary>
        public static void ClickReliablyUntilPageLeave(
            this IWebElement element,
            UITestContext context,
            TimeSpan? timeout = null,
            TimeSpan? interval = null) =>
            context.RetryIfNotStaleOrFail(
                () =>
                {
                    element.ClickReliably(context);
                    return false;
                },
                timeout,
                interval);
    }
}
