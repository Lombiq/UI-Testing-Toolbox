using Atata.HtmlValidation;
using Lombiq.Tests.UI.Helpers;
using Shouldly;
using System;
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
    /// Gets or sets a delegate to run assertions on the <see cref="HtmlValidationResult"/> when HTML validation
    /// happens. Defaults to <see cref="AssertHtmlValidationOutputIsEmptyAsync"/>.
    /// </summary>
    public Func<HtmlValidationResult, Task> AssertHtmlValidationResultAsync { get; set; } = AssertHtmlValidationOutputIsEmptyAsync;

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

        HtmlValidationOptions = HtmlValidationOptions
            .CloneWith(options => options.ConfigPath = Path.Combine(path));

        return this;
    }

    public static readonly Func<HtmlValidationResult, Task> AssertHtmlValidationOutputIsEmptyAsync =
        validationResult =>
        {
            validationResult.Output.ShouldBeEmpty();
            return Task.CompletedTask;
        };

    public static readonly Predicate<UITestContext> EnableOnValidatablePagesHtmlValidationAndAssertionOnPageChangeRule =
        UrlCheckHelper.IsValidatablePage;
}
