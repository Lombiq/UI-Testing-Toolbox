using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Exceptions
{
    public class PageChangeAssertionException : Exception
    {
        public Uri Address { get; set; }
        public string Title { get; set; }

        public PageChangeAssertionException(
            UITestContext context,
            Exception innerException)
            : base(CreateErrorMessage(context), innerException)
        {
            Address = new Uri(context.Driver.Url);
            Title = context.Driver.Title;
        }

        public PageChangeAssertionException()
        {
        }

        public PageChangeAssertionException(string message)
            : base(message)
        {
        }

        public PageChangeAssertionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        private static string CreateErrorMessage(UITestContext context)
        {
            var url = context.Driver.Url;
            var title = context.Driver.Title ?? url;

            return $"Asserting the HTML validation result on page {url}({title}) failed.";
        }
    }
}
