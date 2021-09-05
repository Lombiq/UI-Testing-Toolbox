using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests
{
    public class AnonymousHomePageTests : UITestBase
    {
        public AnonymousHomePageTests(ITestOutputHelper testOutputHelper)
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
    }
}
