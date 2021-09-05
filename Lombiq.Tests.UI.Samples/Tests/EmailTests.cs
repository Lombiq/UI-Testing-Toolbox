using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests
{
    public class EmailTests : UITestBase
    {
        public EmailTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory, Chrome]
        public Task SendTestEmail(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    context.SignInDirectly();

                    context.GoToRelativeUrl("/Admin/Settings/email");

                    context.FillInWithRetries(By.Id("ISite_DefaultSender"), "sender@example.com");
                    context.FillInWithRetries(By.Id("ISite_Host"), context.SmtpServiceRunningContext.Host);
                    context.ClickReliablyOnSubmit();

                    context.ClickReliablyOnUntilPageLeave(By.LinkText("Test settings"));

                    context.FillInWithRetries(By.Id("To"), "recipient@example.com");
                    context.FillInWithRetries(By.Id("Subject"), "Test message");
                    context.FillInWithRetries(By.Id("Body"), "Hi, this is a test.");
                    context.ClickReliablyOnSubmit();

                    context.GoToSmtpWebUI();

                    context.Exists(ByHelper.SmtpInboxRow("Test message"));
                },
                browser,
                // UseSmtpService = true automatically enables the Email module too.
                configuration => configuration.UseSmtpService = true);
    }
}
