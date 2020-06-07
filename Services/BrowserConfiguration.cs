using System;

namespace Lombiq.Tests.UI.Services
{
    public class BrowserConfiguration
    {
        /// <summary>
        /// The browser to use for the current test.
        /// </summary>
        public Browser Browser { get; set; }

        /// <summary>
        /// If set to <c>true</c> the test will use the given browser in headless mode if it there's support for it.
        /// </summary>
        public bool Headless { get; set; } = TestConfigurationManager.GetBoolConfiguration("BrowserConfiguration.Headless") ?? false;

        /// <summary>
        /// This action will be invoked with the browser's options object so you can modify the options as necessary.
        /// </summary>
        public Action<object> BrowserOptionsConfigurator { get; set; }
    }
}
