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
    public class BasicTests : UITestBase
    {
        public BasicTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory, Chrome]
        public Task AnonymousHomePageShouldExist(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    context
                        .Get(By.ClassName("navbar-brand"))
                        .Text
                        .ShouldBe("Lombiq's Open-Source Orchard Core Extensions - UI Testing");

                    context.GetCurrentUserName().ShouldBeNullOrEmpty();
                },
                browser);

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

        [Theory, Chrome]
        public Task FeatureTogglingShouldntCauseError(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    context.EnableFeatureDirectly("OrchardCore.BackgroundTasks");
                    context.DisableFeatureDirectly("OrchardCore.BackgroundTasks");
                },
                browser);
    }
}
