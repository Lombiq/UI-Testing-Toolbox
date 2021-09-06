using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Helpers
{
    public static class UrlCheckHelper
    {
        /// <summary>
        /// Checks if the current page is driven by Orchard, i.e. admin pages or frontend pages that come from built-in
        /// modules.
        /// </summary>
        public static bool IsOrchardPage(UITestContext context)
        {
            var path = context.GetCurrentUri().PathAndQuery;

            return path.StartsWithOrdinalIgnoreCase("/admin") ||
                path.StartsWithOrdinalIgnoreCase("/Login") ||
                path.StartsWithOrdinalIgnoreCase("/ChangePassword") ||
                path.StartsWithOrdinalIgnoreCase("/ExternalLogins");
        }

        /// <summary>
        /// Checks if the current page is suitable to be validated against generic UI testing rules (like HTML markup
        /// validation and accessibility). Pages excluded are where most of the content is not coming from custom
        /// features.
        /// </summary>
        public static bool IsValidatablePage(UITestContext context)
        {
            var url = context.Driver.Url;
            return
                url.ContainsOrdinalIgnoreCase("://localhost:") &&
                !url.StartsWithOrdinalIgnoreCase(context.SmtpServiceRunningContext?.WebUIUri.ToString() ?? "dummy://") &&
                !url.ContainsOrdinalIgnoreCase("Lombiq.Tests.UI.Shortcuts") &&
                !IsOrchardPage(context);
        }

        /// <summary>
        /// A shortcut for the inverse of <see cref="IsValidatablePage(UITestContext)"/>.
        /// </summary>
        public static bool IsNotValidatablePage(UITestContext context) => !IsValidatablePage(context);
    }
}
