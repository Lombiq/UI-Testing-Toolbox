using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class TenantsUITestContextExtensions
{
    public static async Task CreateAndSwitchToTenantManuallyAsync(
        this UITestContext context,
        string name,
        string urlPrefix = "",
        string urlHost = "",
        string featureProfile = "",
        bool navigate = true)
    {
        await context.CreateTenantManuallyAsync(name, urlPrefix, urlHost, featureProfile, navigate);

        await context.ClickReliablyOnByLinkTextAsync("Setup");

        context.SwitchCurrentTenant(name, urlPrefix);
    }

    public static async Task CreateTenantManuallyAsync(
        this UITestContext context,
        string name,
        string urlPrefix = "",
        string urlHost = "",
        string featureProfile = "",
        bool navigate = true)
    {
        if (navigate)
        {
            await context.GoToAdminRelativeUrlAsync("/Tenants");
        }

        await context.ClickReliablyOnByLinkTextAsync("Add Tenant");
        await context.ClickAndFillInWithRetriesAsync(By.Id("Name"), name);

        if (!string.IsNullOrEmpty(urlPrefix))
        {
            await context.ClickAndFillInWithRetriesAsync(By.Id("RequestUrlPrefix"), urlPrefix);
        }

        if (!string.IsNullOrEmpty(urlHost))
        {
            await context.ClickAndFillInWithRetriesAsync(By.Id("RequestUrlHost"), urlHost);
        }

        if (!string.IsNullOrEmpty(featureProfile))
        {
            await context.ClickReliablyOnAsync(By.XPath($"//option[@value='{featureProfile}']"));
        }

        await context.ClickReliablyOnAsync(ByHelper.ButtonText("Create"));
    }
}
