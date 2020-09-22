using OpenQA.Selenium.Remote;

namespace Lombiq.Tests.UI.Services
{
    public class UITestContext
    {
        /// <summary>
        /// Technical name of the current test.
        /// </summary>
        public string TestName { get; }

        /// <summary>
        /// Configuration of the test execution.
        /// </summary>
        public OrchardCoreUITestExecutorConfiguration Configuration { get; }

        /// <summary>
        /// Gets the context for the currently used SQL Server instance and database, if SQL Server is the DB used for
        /// the test.
        /// </summary>
        public SqlServerRunningContext SqlServerRunningContext { get; }

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
            string testName,
            OrchardCoreUITestExecutorConfiguration configuration,
            SqlServerRunningContext sqlServerContext,
            IWebApplicationInstance application,
            AtataScope scope,
            SmtpServiceRunningContext smtpContext)
        {
            TestName = testName;
            Configuration = configuration;
            SqlServerRunningContext = sqlServerContext;
            Application = application;
            Scope = scope;
            SmtpServiceRunningContext = smtpContext;
        }
    }
}
