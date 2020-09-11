using OpenQA.Selenium.Remote;

namespace Lombiq.Tests.UI.Services
{
    public class UITestContext
    {
        /// <summary>
        /// Gets the technical name of the current test.
        /// </summary>
        public string TestName { get; }

        /// <summary>
        /// Gets the configuration of the test execution.
        /// </summary>
        public OrchardCoreUITestExecutorConfiguration Configuration { get; }

        /// <summary>
        /// Gets the web application instance, e.g. an Orchard Core app currently running.
        /// </summary>
        public IWebApplicationInstance Application { get; }

        /// <summary>
        /// Gets a representation of a scope wrapping an Atata-driven UI test.
        /// </summary>
        public AtataScope Scope { get; }

        /// <summary>
        /// Gets the Selenium web driver driving the app via a browser.
        /// </summary>
        public RemoteWebDriver Driver => Scope.Driver;

        /// <summary>
        /// Gets the context for the SMTP service running for the test, if it was requested.
        /// </summary>
        public SmtpServiceRunningContext SmtpServiceRunningContext { get; }


        public UITestContext(
            string testName,
            OrchardCoreUITestExecutorConfiguration configuration,
            IWebApplicationInstance application,
            AtataScope scope,
            SmtpServiceRunningContext smtpContext)
        {
            TestName = testName;
            Configuration = configuration;
            Application = application;
            Scope = scope;
            SmtpServiceRunningContext = smtpContext;
        }
    }
}
