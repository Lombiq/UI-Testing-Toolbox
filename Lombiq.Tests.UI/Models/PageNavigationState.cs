using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Models
{
    /// <summary>
    /// Represents the current web page in terms of whether the browser has navigated away from it yet.
    /// </summary>
    public class PageNavigationState : IWebContentState
    {
        private readonly IWebElement _root;

        public PageNavigationState(IWebElement root) => _root = root;

        public PageNavigationState(UITestContext context)
            : this(context.Get(By.TagName("html")))
        {
        }

        public bool CheckIfNavigationHasOccurred()
        {
            try
            {
                // Just any element operation to cause a StaleElementReferenceException if it's stale. If it isn't
                // then this will always return false.
                return _root.Size.Width < 0;
            }
            catch (StaleElementReferenceException)
            {
                return true;
            }
        }

        public void Wait(TimeSpan? timeout = null, TimeSpan? interval = null) =>
            ReliabilityHelper.DoWithRetriesOrFail(CheckIfNavigationHasOccurred, timeout, interval);
    }
}
