using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.MonkeyTesting;
using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests
{
    public class MonkeyTests : UITestBase
    {
        private readonly MonkeyTestingOptions _monkeyTestingOptions = new()
        {
            PageTestTime = TimeSpan.FromSeconds(10),
            BaseRandomSeed = 1234,
            RunAccessibilityCheckingAssertion = false,
            RunHtmlValidationAssertion = false,
        };

        public MonkeyTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory, Chrome]
        public Task TestCurrentPageAsMonkeyShouldWorkWithRandomSeed(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    context.GoToHomePage();
                    context.TestCurrentPageAsMonkey(_monkeyTestingOptions, 12345);
                },
                browser);

        [Theory, Chrome]
        public Task TestCurrentPageAsMonkeyRecursivelyShouldWorkWithAnonymousUser(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    context.GoToHomePage();
                    context.TestCurrentPageAsMonkeyRecursively(_monkeyTestingOptions);
                },
                browser);

        [Theory, Chrome]
        public Task TestCurrentPageAsMonkeyRecursivelyShouldWorkWithAdminUser(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context =>
                {
                    context.SignInDirectlyAndGoToDashboard();
                    context.TestCurrentPageAsMonkeyRecursively(_monkeyTestingOptions);
                },
                browser);
    }
}
