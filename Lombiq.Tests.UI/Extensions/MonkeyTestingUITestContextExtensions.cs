using Lombiq.Tests.UI.MonkeyTesting;
using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Extensions
{
    /// <summary>
    /// Provides a set of extension methods for monkey testing.
    /// </summary>
    public static class MonkeyTestingUITestContextExtensions
    {
        /// <summary>
        /// Tests the current page as monkey. Test finishes by timeout or when the current page is left during testing.
        /// Can optionally take <paramref name="randomSeed"/> value to reproduce a test with the same randomization.
        /// </summary>
        /// <param name="options">The <see cref="MonkeyTestingOptions"/> instance to configure monkey testing.</param>
        /// <param name="randomSeed">The random seed that is used by the Gremlins.js script.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestCurrentPageAsMonkey(
            this UITestContext context,
            MonkeyTestingOptions options = null,
            int? randomSeed = null)
        {
            var monkeyTester = new MonkeyTester(context, options);
            monkeyTester.TestOnePage(randomSeed);

            return context;
        }

        /// <summary>
        /// Tests the current page as monkey recursively. When the current page is left during test, continues to test
        /// the other page and so on. Each page is tested for <see cref="MonkeyTestingOptions.PageTestTime"/> value of
        /// <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The <see cref="MonkeyTestingOptions"/> instance to configure monkey testing.</param>
        /// <returns>The same <see cref="UITestContext"/> instance.</returns>
        public static UITestContext TestCurrentPageAsMonkeyRecursively(
            this UITestContext context,
            MonkeyTestingOptions options = null)
        {
            var monkeyTester = new MonkeyTester(context, options);
            monkeyTester.TestRecursively();

            return context;
        }
    }
}
