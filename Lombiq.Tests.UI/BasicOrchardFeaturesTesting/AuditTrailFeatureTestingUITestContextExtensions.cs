using Atata;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.BasicOrchardFeaturesTesting;

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
            var auditTrailTestPageTitle = "Audit Trail Test Page";

            await context.EnableFeatureDirectlyAsync("OrchardCore.AuditTrail");
            await context.GoToAdminRelativeUrlAsync("/Settings" + auditTrailPath);

            await context.GoToEditorTabAsync("Content");

            await context.SetCheckboxValueAsync(By.XPath("//input[@value='Page']"));

            await context.ClickReliablyOnSubmitAsync();

            var contentItemsPage = await context.GoToContentItemsPageAsync();
            context.RefreshCurrentAtataContext();
            contentItemsPage
                .CreateNewPage()
                    .Title.Set(auditTrailTestPageTitle)
                    .Publish.ClickAndGo()
                .AlertMessages.Should.Contain(message => message.IsSuccess);

            await context.GoToAdminRelativeUrlAsync(auditTrailPath);

            var auditTrailTestPageSuccessXpath = "//div[contains(@class, eventdata)]/small[contains(., 'was published')" + // #spell-check-ignore-line
                $" and contains(., 'of the Page')]/a[text()='{auditTrailTestPageTitle}']";

            context.Exists(By.XPath(auditTrailTestPageSuccessXpath));

            auditTrailTestPageSuccessXpath = auditTrailTestPageSuccessXpath.Replace("published", "created");

            context.Exists(By.XPath(auditTrailTestPageSuccessXpath));
        });
}
