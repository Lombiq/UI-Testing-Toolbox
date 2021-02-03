namespace OpenQA.Selenium
{
    public static class BasicWebElementExtensions
    {
        /// <summary>
        /// Returns the text content of the <paramref name="element"/> without surrounding whitespace.
        /// </summary>
        public static string GetTextTrimmed(this IWebElement element) => element.Text.Trim();
    }
}
