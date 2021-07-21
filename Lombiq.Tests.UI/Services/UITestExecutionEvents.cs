using OpenQA.Selenium;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services
{
    public delegate Task NavigationEventHandler(UITestContext context, Uri targetUri);

    public delegate Task ClickEventHandler(UITestContext context, IWebElement targeElement);

    public class UITestExecutionEvents
    {
        /// <summary>
        /// Gets or sets the event raised before an explicit navigation to an URL happens.
        /// </summary>
        public NavigationEventHandler BeforeNavigation { get; set; }

        /// <summary>
        /// Gets or sets the event raised after an explicit navigation to an URL happens.
        /// </summary>
        public NavigationEventHandler AfterNavigation { get; set; }

        /// <summary>
        /// Gets or sets the event raised before clicking an element.
        /// </summary>
        public ClickEventHandler BeforeClick { get; set; }

        /// <summary>
        /// Gets or sets the event raised after clicking an element.
        /// </summary>
        public ClickEventHandler AfterClick { get; set; }

        internal UITestExecutionEvents()
        {
        }
    }
}
