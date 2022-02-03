using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Extensions
{
    public static class LocationUITestContextExtensions
    {
        public static string GetPageTitleAndAddress(this UITestContext context)
        {
            if (context.Driver is null) return null;

            var url = context.Driver.Url;
            var title = context.Driver.Title;

            return string.IsNullOrEmpty(title)
                ? url
                : $"{url} ({title})";
        }
    }
}
