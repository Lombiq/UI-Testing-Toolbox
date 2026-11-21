using Atata;
using Lombiq.Tests.UI.Extensions;
using OpenQA.Selenium;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Samples.Tests;

// Sometimes you don't need an actual web browser. Examining static pages of pure HTML can be done with .NET's built-in
// HTTP client and the AngleSharp library Orchard Core already uses. We integrated those into Atata with our custom
// HttpClientDriver. Using it reduces the overhead and makes browsing very fast, especially on remote tests where the
// browser was the primary bottleneck. Note that this driver doesn't know about JavaScript or CSS (e.g. visibility just
// means that the element doesn't have the "hidden" attribute) and browser features like tabs aren't supported.
public class NoBrowserTests : UITestBase
{
    public NoBrowserTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper) =>
        WithoutBrowser();

    [Fact]
    public Task NavigationShouldWork() =>
        RunAsync(async context =>
        {
            // Often you use "WithoutBrowser()" because you really don't need browsing (e.g. for security tests) so to
            // avoid waste, the "HttpClientDriver" doesn't load a page by default. Its initial URL is always
            // "about:blank", so you have to start the test with navigating to the start page. If you have a lot of
            // browserless tests, you can call "AddTest(context => context.GoToHomePageAsync())" in the constructor.
            await context.GoToHomePageAsync();

            // Clicks work only on <a href> elements by navigating to the URL indicated by the "href" attribute.
            await context.ClickReliablyOnAsync(By.LinkText("Man must explore, and this is exploration at its greatest"));

            // You can use all the "Get" methods that use "IWebDriver.FindElements" under the hood.
            context
                .GetAllWhenOneExists(By.ClassName("taxonomy-tag-term"))
                .Select(element => element.GetTextTrimmed())
                .ToArray()
                .ShouldBe(["Earth", "Exploration", "Space"]);

            var compoundBy = By
                .Id("content")
                .Then(By.XPath(".//div[contains(@class, 'field-type-textfield') and contains(. , 'Problems look mighty')]"));
            context.Exists(compoundBy);

            // Navigation methods manipulate the "IWebDriver.Url", so they are all usable as well. However, you can't
            // interact with forms such as login, so you have to use the "SignInDirectly" extension methods.
            await context.SignInDirectlyAndGoToDashboardAsync();
            context.Get(By.TagName("h4")).GetTextTrimmed().ShouldBe("Welcome to Orchard Core");
        });
}

// END OF TRAINING SECTION: Testing without a browser.
