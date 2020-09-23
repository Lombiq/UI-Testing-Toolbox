using Selenium.Axe;
using Shouldly;
using System;

namespace Lombiq.Tests.UI.Services
{
    public class AccessibilityCheckingConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether to create an accessibility report if the given test fails for any reason.
        /// </summary>
        public bool CreateReportOnFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to create an accessibility report for every test. You can use this
        /// to e.g. compile an accessibility report for the whole app, encompassing all pages checked by tests.
        /// </summary>
        public bool CreateReportAlways { get; set; }

        /// <summary>
        /// Gets or sets the (relative or absolute) path where those accessibility reports are stored that are created
        /// for every test (see <see cref="CreateReportAlways"/>).
        /// </summary>
        public string AlwaysCreatedAccessibilityReportsDirectoryPath { get; set; } = "AccessibilityReports";

        /// <summary>
        /// Gets or sets a value indicating whether configuration delegate for the <see cref="AxeBuilder"/> instance
        /// used for accessibility checking. For more information on the various options see
        /// <see href="https://troywalshprof.github.io/SeleniumAxeDotnet/#/?id=axebuilder-reference"/>.
        /// </summary>
        public Action<AxeBuilder> AxeBuilderConfigurator { get; set; }

        public Action<AxeResult> AssertAxeResult { get; set; } = AssertAxeResultIsEmpty;


        public static readonly Action<AxeResult> AssertAxeResultIsEmpty = axeResult =>
        {
            axeResult.Violations.ShouldBeEmpty();
            axeResult.Incomplete.ShouldBeEmpty();
        };
    }
}
