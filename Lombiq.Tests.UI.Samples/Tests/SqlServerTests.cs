using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests
{
    // By default, tests are executed with SQLite. However, you can also run them against a full SQL Server instance too
    // and tests will get their DBs there. Note that for this, you need an SQL Server instance running; by default, this
    // will be attempted under the default localhost server name. If you're using anything else, check out the settings
    // in SqlServerConfiguration.
    // Here we have bsically two of the same tests as in BasicTests but now we're using SQL Server as the site's
    // database.
    public class SqlServerTests : UITestBase
    {
        public SqlServerTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Theory, Chrome]
        public Task AnonymousHomePageShouldExist(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context => context
                    .Get(By.ClassName("navbar-brand"))
                    .Text
                    .ShouldBe("Lombiq's Open-Source Orchard Core Extensions - UI Testing"),
                browser,
                // Note the configuration! We could also set this globally in UITestBase.
                configuration => configuration.UseSqlServer = true);

        [Theory, Chrome]
        public Task TogglingFeaturesShouldWork(Browser browser) =>
            ExecuteTestAfterSetupAsync(
                context => context.ExecuteAndAssertTestFeatureToggle(),
                browser,
                configuration =>
                {
                    configuration.UseSqlServer = true;

                    configuration.AssertBrowserLog =
                        messages =>
                        {
                            var messagesWithoutToggle = messages.Where(message =>
                                !message.IsNotFoundMessage(ShortcutsUITestContextExtensions.FeatureToggleTestBenchUrl));
                            OrchardCoreUITestExecutorConfiguration.AssertBrowserLogIsEmpty(messagesWithoutToggle);
                        };
                });
    }
}

// END OF TRAINING SECTION: Using SQL Server.
