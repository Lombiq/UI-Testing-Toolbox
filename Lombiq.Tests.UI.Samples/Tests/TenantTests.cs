using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// You can also test multi-tenant web apps. Creating tenants on the fly is supported as well with a shortcut. If you'd
// like to test the tenant creation-setup process itself, then look into using CreateNewTenantManuallyAsync() instead.
public class TenantTests : UITestBase
{
    private const string TestTenantName = "Test";
    private const string TestTenantUrlPrefix = "test";
    private const string TestTenantDisplayName = "Lombiq's OSOCE - Test Tenant";

    public TenantTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Theory, Chrome]
    public Task CreatingTenantShouldWork(Browser browser) =>
        ExecuteTestAfterSetupAsync(
            async context =>
            {
                const string tenantAdminName = "tenantAdmin";

                await context.SignInDirectlyAsync();

                // Create the tenant with a custom admin user.
                await context.CreateAndSwitchToTenantAsync(
                    TestTenantName,
                    TestTenantUrlPrefix,
                    new OrchardCoreSetupParameters
                    {
                        SiteName = TestTenantDisplayName,
                        RecipeId = "Lombiq.OSOCE.Tests",
                        TablePrefix = TestTenantUrlPrefix,
                        UserName = tenantAdminName,
                    });

                // Verify successful setup with custom site name.
                context
                    .Get(By.ClassName("navbar-brand"))
                    .Text
                    .ShouldBe(TestTenantDisplayName);

                await context.SignInDirectlyAsync(tenantAdminName);
                (await context.GetCurrentUserNameAsync()).ShouldBe(tenantAdminName);
                context.GetCurrentUri().AbsolutePath.ShouldStartWith($"/{TestTenantUrlPrefix}");

                context.SwitchCurrentTenantToDefault();
                (await context.GetCurrentUserNameAsync()).ShouldBe(DefaultUser.UserName);
                context.GetCurrentUri().AbsolutePath.ShouldNotStartWith($"/{TestTenantUrlPrefix}");
            },
            browser);
}

// END OF TRAINING SECTION: Testing in tenants.
