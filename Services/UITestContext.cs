using OpenQA.Selenium.Remote;

namespace Lombiq.Tests.UI.Services
{
    public class UITestContext
    {
        public OrchardCoreUITestExecutorConfiguration Configuration { get; }

        /// <summary>
        /// The web application instance, e.g. an Orchard Core app currently running.
        /// </summary>
        public IWebApplicationInstance Application { get; }

        /// <summary>
        /// A representation of a scope wrapping an Atata-driven UI test.
        /// </summary>
        public AtataScope Scope { get; }

        /// <summary>
        /// The Selenium web driver driving the app via a browser.
        /// </summary>
        public RemoteWebDriver Driver => Scope.Driver;

        /// <summary>
        /// The context for the SMTP service running for the test, if it was requested.
        /// </summary>
        public SmtpServiceRunningContext SmtpServiceRunningContext { get; }


        public UITestContext(
            OrchardCoreUITestExecutorConfiguration configuration,
            IWebApplicationInstance application,
            AtataScope scope,
            SmtpServiceRunningContext smtpContext)
        {
            Configuration = configuration;
            Application = application;
            Scope = scope;
            SmtpServiceRunningContext = smtpContext;
        }
    }
}
