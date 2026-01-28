using Atata.Cli.HtmlValidate;
using Atata.HtmlValidation;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

/// <summary>
/// Configuration for HTML markup validation. Note that since this uses the html-validate library under the hood further
/// configuration is available via a .htmlvalidate.json file placed into the build output folder, see <see
/// href="https://gitlab.com/html-validate/html-validate/-/tree/master/docs/usage#getting-started">the corresponding
/// docs</see>. A file with recommended default settings is included.
/// </summary>
public class HtmlValidationConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to create an HTML validation report if the given test fails HTML
    /// validation.
    /// </summary>
    public bool CreateReportOnFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets options for Atata.HtmlValidation. Note that since this uses the html-validate library under the
    /// hood further configuration is available via a .htmlvalidate.json file placed into the build output folder, see
    /// <see href="https://gitlab.com/html-validate/html-validate/-/tree/master/docs/usage#getting-started">the
    /// corresponding docs</see>.
    /// </summary>
    public HtmlValidationOptions HtmlValidationOptions { get; set; } = new()
    {
        OutputFormatter = HtmlValidateFormatter.Names.Json,
        SaveHtmlToFile = HtmlSaveCondition.Never,
        SaveResultToFile = true,
        // This is necessary so no long folder names will be generated, see:
        // https://github.com/atata-framework/atata-htmlvalidation/issues/5
        WorkingDirectory = "HtmlValidationTemp",
        // If a consuming project adds a ".htmlvalidate.json" config file then use it, otherwise fall back to the
        // "default.htmlvalidate.json" which always exists because Lombiq.Tests.UI copies it into the directory during
        // build.
        ConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".htmlvalidate.json") is { } rootConfiguration && File.Exists(rootConfiguration)
            ? rootConfiguration
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default.htmlvalidate.json"),
    };

    /// <summary>
    /// Gets or sets a delegate to adjust the <see cref="Atata.HtmlValidation.HtmlValidationOptions"/> instance provided
    /// by <see cref="HtmlValidationOptions"/>.
    /// </summary>
    public Action<HtmlValidationOptions> HtmlValidationOptionsAdjuster { get; set; }

    /// <summary>
    /// Gets a dictionary of filters. The errors from the <see cref="HtmlValidationResult"/> are filtered by
    /// each entry and only those are kept that pass each filter entry.
    /// </summary>
    public IDictionary<string, Func<HtmlValidationError, bool>> HtmlValidationFilters { get; } =
        new Dictionary<string, Func<HtmlValidationError, bool>>();

    /// <summary>
    /// Gets or sets a delegate to run assertions on the <see cref="HtmlValidationResult"/> when HTML validation
    /// happens. If you only want to filter the validation errors, use <see cref="HtmlValidationFilters"/> or <see
    /// cref="WithFilters"/>.
    /// </summary>
    public Func<IList<HtmlValidationError>, Task> AssertHtmlValidationResultAsync { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically run HTML validation every time a page changes (either
    /// due to explicit navigation or clicks) and assert on the validation results.
    /// </summary>
    public bool RunHtmlValidationAssertionOnAllPageChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets a predicate that determines whether HTML validation and asserting the results should run for the
    /// current page. This is only used if <see cref="RunHtmlValidationAssertionOnAllPageChanges"/> is set to <see
    /// langword="true"/>. Defaults to <see cref="EnableOnValidatablePagesHtmlValidationAndAssertionOnPageChangeRule"/>.
    /// </summary>
    public Predicate<UITestContext> HtmlValidationAndAssertionOnPageChangeRule { get; set; } =
        EnableOnValidatablePagesHtmlValidationAndAssertionOnPageChangeRule;

    /// <summary>
    /// Updates the <see cref="HtmlValidationOptions"/>.<see cref="HtmlValidationOptions.ConfigPath"/> with a path
    /// relative to the <see cref="AppDomain.BaseDirectory"/> of the <see cref="AppDomain.CurrentDomain"/> (i.e. the
    /// build directory).
    /// </summary>
    /// <param name="pathSegments">
    /// Directory and file names which are joined together using <see cref="Path.Combine(string[])"/>.
    /// </param>
    public HtmlValidationConfiguration WithRelativeConfigPath(params string[] pathSegments)
    {
        string[] path = [AppDomain.CurrentDomain.BaseDirectory, .. pathSegments];
        HtmlValidationOptions.ConfigPath = Path.Combine(path);

        return this;
    }

    /// <summary>
    /// Updates the <see cref="HtmlValidationFilters"/>.
    /// </summary>
    public HtmlValidationConfiguration WithFilters(string name, Func<HtmlValidationError, bool> filter)
    {
        HtmlValidationFilters[name] = filter;

        return this;
    }

    /// <summary>
    /// Updates the <see cref="HtmlValidationFilters"/> with the <c>OC-15222</c> key to handle a specific bug.
    /// </summary>
    /// <remarks><para>
    /// Rule exclusions due to https://github.com/OrchardCMS/OrchardCore/issues/15222, usages can be removed once it is
    /// resolved.
    /// </para></remarks>
    public HtmlValidationConfiguration WithOC15222Filter() =>
        WithFilters("OC-15222", error =>
            error.RuleId is not ("prefer-native-element" or "text-content" or "no-redundant-role"));

    public static readonly Predicate<UITestContext> EnableOnValidatablePagesHtmlValidationAndAssertionOnPageChangeRule =
        UrlCheckHelper.IsValidatablePage;
}
