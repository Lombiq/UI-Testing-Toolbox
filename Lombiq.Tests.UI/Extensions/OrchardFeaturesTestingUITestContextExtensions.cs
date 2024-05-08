using Atata;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

/// <summary>
/// Provides a set of extension methods for Orchard features testing like Audit Trail, media operations and workflows.
/// </summary>
public static class OrchardFeaturesTestingUITestContextExtensions
{
    public static Task TestMediaOperationsAsync(this UITestContext context) =>
        context.ExecuteTestAsync(
            "Test media operations",
            async () =>
            {
                const string mediaPath = "/Media";
                var imageName = FileUploadHelper.SamplePngFileName;
                var documentName = FileUploadHelper.SamplePdfFileName;

                await context.GoToAdminRelativeUrlAsync(mediaPath);

                context.UploadSamplePngByIdOfAnyVisibility("fileupload"); // #spell-check-ignore-line

                // Workaround for pending uploads, until you make an action the page is stuck on "Uploads Pending".
                context.WaitForPageLoad();
                await context.ClickReliablyOnAsync(By.CssSelector("body"));

                context.Exists(By.XPath($"//span[contains(text(), '{imageName}')]"));

                await context
                    .Get(By.CssSelector($"a[href=\"/media/{imageName}\"]").OfAnyVisibility())
                    .ClickReliablyAsync(context);
                context.SwitchToFirstWindow();

                context.WaitForPageLoad();
                await context.GoToAdminRelativeUrlAsync(mediaPath);

                context.UploadSamplePdfByIdOfAnyVisibility("fileupload"); // #spell-check-ignore-line

                // Workaround for pending uploads, until you make an action the page is stuck on "Uploads Pending".
                context.WaitForPageLoad();
                await context.ClickReliablyOnAsync(By.CssSelector("body"));

                context.Exists(By.XPath($"//span[contains(text(), '{documentName}')]"));

                await context
                    .Get(By.XPath($"//span[contains(text(), '{documentName}')]/ancestor::tr").OfAnyVisibility())
                    .ClickReliablyAsync(context);

                await context
                    .Get(By.CssSelector($"a[href=\"/media/{documentName}\"]"))
                    .ClickReliablyAsync(context);
                context.SwitchToFirstWindow();

                context.WaitForPageLoad();
                await context.GoToAdminRelativeUrlAsync(mediaPath);

                await context
                    .Get(By.CssSelector("#folder-tree .treeroot .folder-actions")) // #spell-check-ignore-line
                    .ClickReliablyAsync(context);

                context.Get(By.Id("create-folder-name")).SendKeys("Example Folder");

                await context.ClickReliablyOnAsync(By.Id("modalFooterOk"));

                // Wait until new folder is created.
                context.Exists(
                    By.XPath("//div[contains(@class, 'alert-info') and contains(.,'This folder is empty')]"));

                context.UploadSamplePngByIdOfAnyVisibility("fileupload"); // #spell-check-ignore-line
                context.UploadSamplePdfByIdOfAnyVisibility("fileupload"); // #spell-check-ignore-line
                context.WaitForPageLoad();

                var image = context.Get(By.XPath($"//span[contains(text(), '{imageName}')]"));

                context.Exists(By.XPath($"//span[contains(text(), '{documentName}')]"));

                await image.ClickReliablyAsync(context);

                await context
                    .Get(By.XPath($"//span[contains(text(), '{imageName}')]/ancestor::tr"))
                    .Get(By.CssSelector("a.btn.btn-link.btn-sm.delete-button"))
                    .ClickReliablyAsync(context);

                await context.ClickModalOkAsync();
                context.WaitForPageLoad();
                await context.GoToAdminRelativeUrlAsync(mediaPath);

                context.Missing(By.XPath("//span[text()=' Image.png ' and @class='break-word']"));

                var deleteFolderButton =
                    context.Get(By.CssSelector("#folder-tree  li.selected  div.btn-group.folder-actions .svg-inline--fa.fa-trash"));
                await deleteFolderButton.ClickReliablyAsync(context);

                await context.ClickModalOkAsync();
                context.WaitForPageLoad();
                await context.GoToAdminRelativeUrlAsync(mediaPath);

                context.Missing(By.XPath("//div[text()='Example Folder' and @class='folder-name ms-2']"));
            });

    public static Task TestAuditTrailAsync(this UITestContext context) =>
    context.ExecuteTestAsync(
        "Test Audit Trail",
        async () =>
        {
            var auditTrailPath = "/AuditTrail";

            await context.EnableFeatureDirectlyAsync("OrchardCore.AuditTrail");
            await context.GoToAdminRelativeUrlAsync("/Settings" + auditTrailPath);

            await context.GoToEditorTabAsync("Content");

            await context.SetCheckboxValueAsync(By.XPath("//input[@value='Page']"), isChecked: true);

            await context.ClickReliablyOnSubmitAsync();

            var contentItemsPage = await context.GoToContentItemsPageAsync();
            context.RefreshCurrentAtataContext();
            contentItemsPage
                .CreateNewPage()
                    .Title.Set("Audit Trail")
                    .Publish.ClickAndGo()
                .AlertMessages.Should.Contain(message => message.IsSuccess);

            await context.GoToAdminRelativeUrlAsync(auditTrailPath);

            context.Exists(ByHelper.TextContains("of the Page"));

            context.Exists(ByHelper.TextContains("was published"));

            context.Exists(ByHelper.TextContains("was created"));
        });

    public static Task TestWorkflowsAsync(this UITestContext context) =>
        context.ExecuteTestAsync(
            "Test Workflows",
            async () =>
            {
                var workflowsPath = "/Workflows/Types";
                var contentItemPublishTestSuccessMessage = "The content item was published.";

                await context.EnableFeatureDirectlyAsync("OrchardCore.Workflows");
                await context.GoToAdminRelativeUrlAsync(workflowsPath + "/EditProperties");

                await context.ClickAndFillInWithRetriesAsync(By.Id("Name"), "Test workflow");
                await context.ClickReliablyOnSubmitAsync();

                await context.ClickReliablyOnAsync(By.XPath("//button[@data-activity-type='Event']"));
                await context.ClickReliablyOnAsync(By.XPath("//a[contains(@href, 'ContentPublishedEvent')]"));

                await context.ClickAndFillInWithRetriesAsync(By.Id("IActivity_Title"), "Content Published Trigger");
                await context.SetCheckboxValueAsync(By.XPath("//input[@value='Page']"), isChecked: true);
                await context.ClickReliablyOnSubmitAsync();

                await context.ClickReliablyOnAsync(By.XPath("//button[@data-activity-type='Task']"));
                await context.ClickReliablyOnAsync(By.XPath("//a[contains(@href, 'NotifyTask')]"));

                await context.ClickAndFillInWithRetriesAsync(By.Id("IActivity_Title"), "Content Published Notification");
                await context.ClickAndFillInWithRetriesAsync(By.Id("NotifyTask_Message"), contentItemPublishTestSuccessMessage);
                await context.ClickReliablyOnSubmitAsync();

                var taskXPath = "//div[contains(@class, 'activity-task')]";
                var eventXPath = "//div[contains(@class, 'activity-event')]";

                context.DragAndDropToOffset(By.XPath(taskXPath), 400, 0);

                context.DragAndDrop(
                    By.XPath(eventXPath + "//circle"),
                    By.XPath(taskXPath + "//circle"));

                await context.ClickReliablyOnAsync(By.XPath(eventXPath));
                await context.ClickReliablyOnAsync(By.XPath("//a[@title='Startup task']"));
                await context.ClickReliablyOnSubmitAsync();

                context.ShouldBeSuccess("Workflow has been saved.");

                var contentItemsPage = await context.GoToContentItemsPageAsync();
                context.RefreshCurrentAtataContext();
                contentItemsPage
                    .CreateNewPage()
                        .Title.Set("Workflows")
                        .Publish.ClickAndGo();

                context.ShouldBeSuccess(contentItemPublishTestSuccessMessage);

                // Checking if the workflow run was logged.
                await context.GoToAdminRelativeUrlAsync(workflowsPath);
                await context.ClickReliablyOnAsync(By.XPath("//a[contains(@href, 'Instances')]"));
                context.Exists(By.XPath("//a[@class = 'badge text-bg-success']"));
            });
}
