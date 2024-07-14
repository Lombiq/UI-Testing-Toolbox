using Atata;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.BasicOrchardFeaturesTesting;

/// <summary>
/// Provides a set of extension methods for testing Orchard Core media operations.
/// </summary>
public static class MediaOperationsTestingUITestContextExtensions
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
                // Closing the newly opened tab with the image, so the browser doesn't continue to switch the UI back
                // and forth.
                context.SwitchToLastWindow();
                context.Driver.Close();
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
                context.SwitchToLastWindow();
                context.Driver.Close();
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
}
