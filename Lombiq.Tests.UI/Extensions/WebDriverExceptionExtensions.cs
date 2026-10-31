using OpenQA.Selenium.Chrome;

namespace OpenQA.Selenium;

public static class WebDriverExceptionExtensions
{
    /// <summary>
    /// Checks if the exception is one that's thrown when trying to access an element that's stale.
    /// </summary>
    /// <remarks><para>
    /// Detects <see cref="StaleElementReferenceException"/> and instances of <see cref="UnknownErrorException"/>, which
    /// is sometimes thrown by <see cref="ChromeDriver"/> instead of the former. See
    /// https://stackoverflow.com/questions/76250688/webdriverexception-unhandled-inspector-error-no-node-with-given-id
    /// for more details.
    /// </para></remarks>
    public static bool IsStaleElementLikeException(this WebDriverException exception) =>
        exception is StaleElementReferenceException ||
        (exception is UnknownErrorException ex && ex.Message.Contains("Node with given id does not belong to the document"));
}
