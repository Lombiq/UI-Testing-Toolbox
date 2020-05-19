using OpenQA.Selenium.Remote;

namespace Lombiq.Tests.UI.Services
{
    public class UITestContext
    {
        public IWebApplicationInstance Application { get; }
        public AtataScope Scope { get; }
        public RemoteWebDriver Driver => Scope.Driver;
        public SmtpServiceRunningContext SmtpServiceRunningContext { get; }


        public UITestContext(IWebApplicationInstance application, AtataScope scope, SmtpServiceRunningContext smtpContext)
        {
            Application = application;
            Scope = scope;
            SmtpServiceRunningContext = smtpContext;
        }
    }
}
