namespace OpenQA.Selenium;

public static class WebDriverExceptionExtensions
{
    /// <summary>
    /// Checks if the exception is one that's thrown when trying to access an element that's stale.
    /// </summary>
    public static bool IsStateElementLikeException(this WebDriverException exception) =>
        exception is StaleElementReferenceException ||
        // This is the same as StaleElementReferenceException but for some reason ChromeDriver randomly throws this
        // instead. Also see:
        // https://stackoverflow.com/questions/76250688/webdriverexception-unhandled-inspector-error-no-node-with-given-id-found-at-a.
        (exception is UnknownErrorException ex && ex.Message.Contains("Node with given id does not belong to the document"));
}
