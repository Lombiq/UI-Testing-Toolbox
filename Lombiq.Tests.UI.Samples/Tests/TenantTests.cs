using Lombiq.Tests.UI.Attributes;
using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using Shouldly;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Lombiq.Tests.UI.Samples.Tests;

// Testing in multi-tenancy context is not yet fully supported (https://github.com/Lombiq/UI-Testing-Toolbox/issues/80)
// but you can create sites, and features that don't navigate to an absolute path work. If you have to navigate, either
// use context.GoToRelativeUrlAsync() but remember to use include the tenant url prefix, or change context.TenantName
// (context.CreateAndEnterTenantAsync() already does this) and then use context.GoToAsync<TController>() to
// navigate by MVC actions.
public class TenantTests : UITestBase
{
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
                await context.CreateAndEnterTenantAsync(
                    TestTenantUrlPrefix,
                    TestTenantUrlPrefix,
                    "Lombiq.OSOCE.Tests",
                    new TenantSetupParameters
                    {
                        UserName = tenantAdminName,
                        SiteName = TestTenantDisplayName,
                    });

                // Verify successful setup with custom site name.
                context
                    .Get(By.ClassName("navbar-brand"))
                    .Text
                    .ShouldBe(TestTenantDisplayName);

                await context.SignInDirectlyAsync(tenantAdminName);
                (await context.GetCurrentUserNameAsync()).ShouldBe(tenantAdminName);
                context.GetCurrentUri().AbsolutePath.ShouldStartWith($"/{TestTenantUrlPrefix}");

                context.TenantName = string.Empty;
                (await context.GetCurrentUserNameAsync()).ShouldBe(DefaultUser.UserName);
                context.GetCurrentUri().AbsolutePath.ShouldNotStartWith($"/{TestTenantUrlPrefix}");
            },
            browser);
}

// END OF TRAINING SECTION: Testing in tenants.
