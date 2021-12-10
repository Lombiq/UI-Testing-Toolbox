using Lombiq.Tests.UI.MonkeyTesting;
using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Extensions
{
    public static class MonkeyTestingUITestContextExtensions
    {
        public static UITestContext TestCurrentPageAsMonkey(this UITestContext context, MonkeyTestingOptions options = null, int? randomSeed = null)
        {
            var monkeyTester = new MonkeyTester(context, options);
            monkeyTester.TestOnePage(randomSeed);

            return context;
        }

        public static UITestContext TestCurrentPageAsMonkeyRecursively(this UITestContext context, MonkeyTestingOptions options = null)
        {
            var monkeyTester = new MonkeyTester(context, options);
            monkeyTester.TestRecursively();

            return context;
        }
    }
}
