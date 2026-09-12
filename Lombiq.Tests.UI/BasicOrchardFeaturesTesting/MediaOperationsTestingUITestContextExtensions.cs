using Atata;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;
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

                context.UploadSamplePngByIdOfAnyVisibility("fileupload");

                // Workaround for pending uploads, until you make an action the page is stuck on "Uploads Pending".
                context.WaitForPageLoad();
                await context.ClickReliablyOnAsync(By.CssSelector("body"));

                // Check for errors before validating the result to aim for most useful information in case of errors.
                await context.AssertLogsAsync();
                context.Missing(By.CssSelector("#mediaContainerMain .upload-list .text-danger"));
                context.Exists(By.XPath($"//span[contains(text(), '{imageName}')]"));

                await context.ClickReliablyOnAsync(
                    By.CssSelector($"a[href^=\"{context.UrlPrefix}/media/{imageName}\"]").OfAnyVisibility());

                // Closing the newly opened tab with the image, so the browser doesn't continue to switch the UI back
                // and forth.
                context.DoWithRetriesOrFail(
                    () => context.Driver.WindowHandles.Count > 1,
                    TimeSpan.FromSeconds(30));
                context.SwitchToLastWindow();
                context.Driver.Close();
                context.SwitchToLastWindow();

                context.WaitForPageLoad();
                await context.GoToAdminRelativeUrlAsync(mediaPath);

                context.UploadSamplePdfByIdOfAnyVisibility("fileupload");

                // Workaround for pending uploads, until you make an action the page is stuck on "Uploads Pending".
                context.WaitForPageLoad();
                await context.ClickReliablyOnAsync(By.CssSelector("body"));

                // For some reason, the PDF window in Chrome can't be closed (context.Driver.Close() will just time
                // out). Thus not doing opening and closing it as with the image above.
                context.Exists(By.XPath($"//span[contains(text(), '{documentName}')]"));

                await context.ClickReliablyOnAsync(
                    By.XPath($"//span[contains(text(), '{documentName}')]/ancestor::tr").OfAnyVisibility());

                context.WaitForPageLoad();
                await context.GoToAdminRelativeUrlAsync(mediaPath);

                await context.ClickReliablyOnAsync(By.CssSelector("#folder-tree .treeroot .folder-actions"));

                context.Get(By.Id("create-folder-name")).SendKeys("Example Folder");

                await context.ClickReliablyOnAsync(By.Id("modalFooterOk"));

                // Wait until new folder is created.
                await context.DoWithRetriesOrFailAsync(
                    () => Task.FromResult(context.Exists(By
                        .XPath("//div[contains(@class, 'alert-info') and contains(.,'This folder is empty')]")
                        .Safely())),
                    timeout: TimeSpan.FromMinutes(2));

                context.UploadSamplePngByIdOfAnyVisibility("fileupload");
                context.UploadSamplePdfByIdOfAnyVisibility("fileupload");
                context.WaitForPageLoad();

                var byImage = ByHelper.TextContains(imageName, "span");
                var image = context.Get(byImage);

                context.Exists(By.XPath($"//span[contains(text(), '{documentName}')]"));

                await image.ClickReliablyAsync(context, byImage);

                await context.ClickReliablyOnAsync(
                    By.XPath($"//span[contains(text(), '{imageName}')]/ancestor::tr//a[contains(@class, 'delete-button')]"));

                await context.ClickModalOkAsync();
                context.WaitForPageLoad();
                await context.GoToAdminRelativeUrlAsync(mediaPath);

                context.Missing(By.XPath($"//span[text()=' {imageName} ' and @class='break-word']"));

                await context.ClickReliablyOnAsync(By.CssSelector("#folder-tree .folder-actions .fa-trash"));

                await context.ClickModalOkAsync();
                context.WaitForPageLoad();
                await context.GoToAdminRelativeUrlAsync(mediaPath);

                context.Missing(By.XPath("//div[text()='Example Folder' and @class='folder-name ms-2']"));
            });
}
