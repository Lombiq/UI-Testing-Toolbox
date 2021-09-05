using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests
{
    public class LoginTests : UITestBase
    {
        public LoginTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory, Chrome]
        public Task LoginShouldWork(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    context.GoToRelativeUrl("/Login");

                    context.FillInWithRetries(By.Id("UserName"), DefaultUser.UserName);
                    context.FillInWithRetries(By.Id("Password"), DefaultUser.Password);

                    context.ClickReliablyOn(By.CssSelector("button[type='submit']"));

                    context.GetCurrentUserName().ShouldBe(DefaultUser.UserName);
                },
                browser);
    }
}
