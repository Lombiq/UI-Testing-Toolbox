using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Samples.Extensions
{
    public static class UITestContextExtensions
    {
        public static async Task CheckIfAnonymousHomePageExistsAsync(this UITestContext context)
        {
            // Is the title correct?
            context
                .Get(By.ClassName("navbar-brand"))
                .Text
                .ShouldBe("Lombiq's OSOCE - UI Testing");

            // Are we logged out?
            (await context.GetCurrentUserNameAsync()).ShouldBeNullOrEmpty();
        }
    }
}
