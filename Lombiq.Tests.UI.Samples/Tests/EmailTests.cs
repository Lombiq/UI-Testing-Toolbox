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
    // In this test class we'll work with (wait for it!) e-mails. The UI Testing Toolbox provides services to run an
    // SMTP server locally that the app can use to send out e-mails, which we can then immediately check.
    public class EmailTests : UITestBase
    {
        public EmailTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory, Chrome]
        public Task SendingTestEmailShouldWork(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                async context =>
                {
                    // A shortcut to sign in without going through (and thus testing) the login screen.
                    await context.SignInDirectlyAsync();

                    // Let's go to the e-mail admin page.
                    await context.GoToRelativeUrlAsync("/Admin/Settings/email");

                    // The default sender is configured in the test recipe so we can use the test feature.
                    context.ClickReliablyOnUntilPageLeave(By.LinkText("Test settings"));

                    // Let's send a basic e-mail.
                    await context.FillInWithRetriesAsync(By.Id("To"), "recipient@example.com");
                    await context.FillInWithRetriesAsync(By.Id("Subject"), "Test message");
                    await context.FillInWithRetriesAsync(By.Id("Body"), "Hi, this is a test.");
                    await context.ClickReliablyOnSubmitAsync();

                    // The SMTP service running behind the scenes also has a web UI that we can access to see all
                    // outgoing e-mails and check if everything's alright.
                    await context.GoToSmtpWebUIAsync();

                    // If the e-mail we've sent exists then it's all good.
                    context.Exists(ByHelper.SmtpInboxRow("Test message"));
                },
                browser,
                // UseSmtpService = true automatically enables the Email module too so you don't have to enable it in
                // a recipe.
                configuration => configuration.UseSmtpService = true);
    }
}

// END OF TRAINING SECTION: E-mail tests.
// NEXT STATION: Head over to Tests/AccessibilityTest.cs.
