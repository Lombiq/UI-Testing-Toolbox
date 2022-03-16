using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Helpers;

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
            path.StartsWithOrdinalIgnoreCase("/Register") ||
            path.StartsWithOrdinalIgnoreCase("/Login") ||
            path.StartsWithOrdinalIgnoreCase("/ChangePassword") ||
            path.StartsWithOrdinalIgnoreCase("/ExternalLogins") ||
            context.IsSetupPage();
    }

    /// <summary>
    /// Checks if the current page is suitable to be validated against generic UI testing rules (like HTML markup
    /// validation and accessibility). Pages excluded are where most of the content is not coming from custom
    /// features.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The excluded localhost pages are where any generic validation is pointless (for shortcuts) or where most of
    /// or a large portion of the HTML output is generated by Orchard modules. Since both HTML validation and
    /// accessibility checking happens for the full HTML output by default (although this can be adjusted but it
    /// needs to be done in an application-specific way) we exclude those by default, not to validate Orchard
    /// features.
    /// </para>
    /// <para>
    /// Note that while this is used by default, you can change this configuration (and how such validation happens
    /// in general) in the respective configuration classes, e.g. <see cref="HtmlValidationConfiguration"/> and <see
    /// cref="AccessibilityCheckingConfiguration"/>.
    /// </para>
    /// </remarks>
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
