using Selenium.Axe;
using Shouldly;
using System;

namespace Lombiq.Tests.UI.Services
{
    public class AccessibilityCheckingConfiguration
    {
        public bool CreateReportOnFailure { get; set; } = true;

        /// <summary>
        /// Configuration delegate for the <see cref="AxeBuilder"/> instance used for accessibility checking. For more
        /// information on the various options see 
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
