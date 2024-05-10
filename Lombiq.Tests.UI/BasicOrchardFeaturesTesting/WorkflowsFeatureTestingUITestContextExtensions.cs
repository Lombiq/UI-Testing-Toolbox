using Atata;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.BasicOrchardFeaturesTesting;

/// <summary>
/// Provides a set of extension methods for Orchard Core Workflows feature testing.
/// </summary>
public static class WorkflowsFeatureTestingUITestContextExtensions
{
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
                await context.SetCheckboxValueAsync(By.XPath("//input[@value='Page']"));
                await context.ClickReliablyOnSubmitAsync();

                await context.ClickReliablyOnAsync(By.XPath("//button[@data-activity-type='Task']"));
                await context.ClickReliablyOnAsync(By.XPath("//a[contains(@href, 'NotifyTask')]"));

                await context.ClickAndFillInWithRetriesAsync(By.Id("IActivity_Title"), "Content Published Notification");
                await context.ClickAndFillInWithRetriesAsync(By.Id("NotifyTask_Message"), contentItemPublishTestSuccessMessage);
                await context.ClickReliablyOnSubmitAsync();

                var taskXPath = "//div[contains(@class, 'activity-task')]";

                context.DragAndDropToOffset(By.XPath(taskXPath), 400, 0);

                context.DragAndDrop(
                    By.XPath("//div[@class = 'jtk-endpoint jtk-endpoint-anchor jtk-draggable jtk-droppable']"), // #spell-check-ignore-line
                    By.XPath(taskXPath));

                // We need to save the workflow early, because sometimes the editor, thus the startup task button can be
                // buggy during UI testing (it won't be clicked, even if we check for its existence). This way it's
                // always clicked.
                await context.ClickReliablyOnSubmitAsync();
                await context.ClickReliablyOnAsync(By.XPath("//div[contains(@class, 'activity-event')]"));

                await context.ClickReliablyOnAsync(By.XPath("//a[@title='Startup task']"));
                await context.ClickReliablyOnSubmitAsync();

                context.ShouldBeSuccess("Workflow has been saved.");

                var contentItemsPage = await context.GoToContentItemsPageAsync();
                context.RefreshCurrentAtataContext();
                contentItemsPage
                    .CreateNewPage()
                        .Title.Set("Workflows Test Page")
                        .Publish.ClickAndGo();

                context.ShouldBeSuccess(contentItemPublishTestSuccessMessage);

                // Checking if the workflow run was logged.
                await context.GoToAdminRelativeUrlAsync(workflowsPath);
                await context.ClickReliablyOnAsync(By.XPath("//a[text()='Test workflow']/following-sibling::a[contains(@href, 'Instances')]"));
                context.Exists(By.XPath("//span[@class = 'badge text-bg-success']"));
            });
}
