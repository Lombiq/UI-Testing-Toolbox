using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class MediaLibraryUITestContextExtensions
{
    /// <summary>
    /// Selects a file from the Media Library for a media field.
    /// </summary>
    /// <param name="fieldId">ID of the media field.</param>
    /// <param name="folderName">The folder containing the desired file.</param>
    /// <param name="fileName">The file to select.</param>
    /// <param name="useRootFolder">Determines whether to search for the file in the root Media Library folder.</param>
    public static async Task SetMediaFieldUsingExistingFileFromMediaLibraryAsync(
        this UITestContext context,
        string fieldId,
        string folderName,
        string fileName,
        bool useRootFolder = false)
    {
        await context.ClickReliablyOnAsync(By.XPath($"//div[@id='{fieldId}']//a[@class='btn btn-secondary btn-sm']"));

        if (!useRootFolder)
        {
            await context.ClickReliablyOnAsync(
                By.XPath($"//a[@class='folder-menu-item']//div[contains(., '{folderName}')]"));
        }

        await context.ClickReliablyOnAsync(By.XPath($"//span[contains(., '{fileName}')]"));
        await context.ClickReliablyOnAsync(By.XPath("//button[@class='btn btn-primary mediaFieldSelectButton']"));
    }
}
