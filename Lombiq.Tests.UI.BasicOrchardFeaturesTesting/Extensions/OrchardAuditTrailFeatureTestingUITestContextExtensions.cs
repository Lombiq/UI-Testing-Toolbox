using Atata;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.BasicOrchardFeaturesTesting.Extensions;

/// <summary>
/// Provides a set of extension methods for Orchard Core Audit Trail feature testing.
/// </summary>
public static class AuditTrailFeatureTestingUITestContextExtensions
{
    public static Task TestAuditTrailAsync(this UITestContext context) =>
    context.ExecuteTestAsync(
        "Test Audit Trail",
        async () =>
        {
            var auditTrailPath = "/AuditTrail";

            await context.EnableFeatureDirectlyAsync("OrchardCore.AuditTrail");
            await context.GoToAdminRelativeUrlAsync("/Settings" + auditTrailPath);

            await context.GoToEditorTabAsync("Content");

            await context.SetCheckboxValueAsync(By.XPath("//input[@value='Page']"));

            await context.ClickReliablyOnSubmitAsync();

            var contentItemsPage = await context.GoToContentItemsPageAsync();
            context.RefreshCurrentAtataContext();
            contentItemsPage
                .CreateNewPage()
                    .Title.Set("Audit Trail Test Page")
                    .Publish.ClickAndGo()
                .AlertMessages.Should.Contain(message => message.IsSuccess);

            await context.GoToAdminRelativeUrlAsync(auditTrailPath);

            context.Exists(ByHelper.TextContains("of the Page"));

            context.Exists(ByHelper.TextContains("was published"));

            context.Exists(ByHelper.TextContains("was created"));

            context.Exists(ByHelper.TextContains("Audit Trail Test Page"));
        });
}
