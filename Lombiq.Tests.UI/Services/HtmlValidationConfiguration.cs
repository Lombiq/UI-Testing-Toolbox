using Atata.HtmlValidation;
using Shouldly;
using System;

namespace Lombiq.Tests.UI.Services
{
    /// <summary>
    /// Configuration for HTML markup validation. Note that since this uses the html-validate library under the hood
    /// further configuration is available via a .htmlvalidate.json file placed into the build output folder, see <see
    /// href="https://gitlab.com/html-validate/html-validate/-/tree/master/docs/usage#getting-started">the corresponding
    /// docs</see>.
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
        /// hood further configuration is available via a .htmlvalidate.json file placed into the build output folder,
        /// see <see href="https://gitlab.com/html-validate/html-validate/-/tree/master/docs/usage#getting-started">the
        /// corresponding docs</see>.
        /// </summary>
        public HtmlValidationOptions HtmlValidationOptions { get; set; } = new HtmlValidationOptions
        {
            SaveResultToFile = true,
        };

        /// <summary>
        /// Gets or sets a delegate to adjust the <see cref="Atata.HtmlValidation.HtmlValidationOptions"/> instance
        /// provided by <see cref="HtmlValidationOptions"/>.
        /// </summary>
        public Action<HtmlValidationOptions> HtmlValidationOptionsAdjuster { get; set; }

        /// <summary>
        /// Gets or sets a delegate to run assertions on the <see cref="HtmlValidationResult"/> when HTML validation
        /// happens.
        /// </summary>
        public Action<HtmlValidationResult> AssertHtmlValidationResult { get; set; } = AssertHtmlValidationOutputIsEmpty;

        public static readonly Action<HtmlValidationResult> AssertHtmlValidationOutputIsEmpty =
            validationResult => validationResult.Output.ShouldBeEmpty();
    }
}
