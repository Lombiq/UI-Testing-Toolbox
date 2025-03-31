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
                const string testWorkflowName = "Test workflow";
                const string workflowsPath = "/Workflows/Types";
                const string contentItemPublishTestSuccessMessage = "The content item was published.";

                await context.EnableFeatureDirectlyAsync("OrchardCore.Workflows");
                await context.GoToAdminRelativeUrlAsync(workflowsPath + "/EditProperties");

                await context.ClickAndFillInWithRetriesAsync(By.Id("Name"), testWorkflowName);
                await context.ClickReliablyOnSubmitAsync();

                await context.ClickReliablyOnAsync(By.XPath("//button[@data-activity-type='Event']"));
                // Make sure that the Content Published event's card loads before trying to add it.
                context.Exists(By.XPath("//h4[contains(@class, 'card-title') and contains(text(), 'Content Published')]"));
                await context.ClickReliablyOnUntilNavigationHasOccurredAsync(By.XPath("//a[contains(@href, 'ContentPublishedEvent')]"));

                await context.ClickAndFillInWithRetriesAsync(By.Id("IActivity_ActivityMetadata_Title"), "Content Published Trigger");
                await context.SetCheckboxValueAsync(By.XPath("//input[@value='Page']"));
                await context.ClickReliablyOnSubmitAsync();
                context.ShouldBeSuccess("Activity added successfully.");

                await context.ClickReliablyOnAsync(By.XPath("//button[@data-activity-type='Task']"));
                var notifyBy = By.XPath("//h4[contains(@class, 'card-title') and contains(text(), 'Notify')]");
                // Make sure that the Notify task's card loads before trying to add it.
                context.Exists(notifyBy);
                // The Add button may be out of the viewport, what makes clicking it unreliable.
                context.ScrollTo(notifyBy);
                await context.ClickReliablyOnUntilNavigationHasOccurredAsync(By.XPath("//a[contains(@href, 'NotifyTask')]"));

                await context.ClickAndFillInWithRetriesAsync(By.Id("IActivity_ActivityMetadata_Title"), "Content Published Notification");
                await context.ClickAndFillInWithRetriesAsync(By.Id("NotifyTask_Message"), contentItemPublishTestSuccessMessage);
                await context.ClickReliablyOnSubmitAsync();
                context.ShouldBeSuccess("Activity added successfully.");

                var taskXPath = "//div[contains(@class, 'activity-task')]";

                context.DragAndDropToOffset(By.XPath(taskXPath), 400, 0);

                context.DragAndDrop(
                    By.XPath("//div[@class = 'jtk-endpoint jtk-endpoint-anchor jtk-draggable jtk-droppable']"),
                    By.XPath(taskXPath));

                context.WaitElementToNotChange(By.ClassName("jtk-connector"));

                // We need to save the workflow early, because sometimes the editor, thus the startup task button can be
                // buggy during UI testing (it won't be clicked, even if we check for its existence). This way it's
                // always clicked.
                await context.ClickReliablyOnSubmitAsync("Save");
                context.ShouldBeSuccess("Workflow has been saved.");

                await context.ClickReliablyOnAsync(By.XPath("//div[contains(@class, 'activity-event')]"));

                await context.ClickReliablyOnAsync(By.XPath("//a[@title='Startup event']"));

                // Sometimes during this step the test can fail, so we are using JavaScript to click submit, this only
                // happens here.
                await context.ClickReliablyOnSubmitAsync("Save", withJavaScript: true);
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

                // If we are testing an app with already existing workflows, our "Test workflow" can end up on a
                // different page, rather than on the first.
                await context.FilterOnAdminAsync(testWorkflowName);

                await context.ClickReliablyOnAsync(By.XPath("//a[text()='Test workflow']/following-sibling::a[contains(@href, 'Instances')]"));
                context.Exists(By.XPath("//span[@class = 'badge text-bg-success']"));
            });
}
