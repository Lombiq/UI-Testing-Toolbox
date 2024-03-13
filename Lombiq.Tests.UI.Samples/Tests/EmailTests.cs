using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// In this test class we'll work with (wait for it!) e-mails. The UI Testing Toolbox provides services to run an SMTP
// server locally that the app can use to send out e-mails, which we can then immediately check.
public class EmailTests : UITestBase
{
    public EmailTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public Task SendingTestEmailShouldWork() =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                // A shortcut to sign in without going through (and thus testing) the login screen.
                await context.SignInDirectlyAsync();

                // Let's go to the "Test settings" option of the e-mail admin page and send a basic e-mail. The default
                // sender is configured in the test recipe so we can use the test feature.
                await context.GoToEmailTestAsync();
                await context.FillEmailTestFormAsync("Test message");
                context.ShouldBeSuccess();

                // The SMTP service running behind the scenes also has a web UI that we can access to see all outgoing
                // e-mails and check if everything's alright.
                await context.GoToSmtpWebUIAsync();

                // If the e-mail we've sent exists then it's all good.
                context.Exists(ByHelper.SmtpInboxRow("Test message"));
            },
            // UseSmtpService = true automatically enables the Email module too so you don't have to enable it in a
            // recipe.
            configuration => configuration.UseSmtpService = true);
}

// END OF TRAINING SECTION: E-mail tests.
// NEXT STATION: Head over to Tests/AccessibilityTest.cs.
