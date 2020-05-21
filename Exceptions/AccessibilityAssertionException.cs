using Selenium.Axe;
using System;

namespace Lombiq.Tests.UI.Exceptions
{
    public class AccessibilityAssertionException : Exception
    {
        public AxeResult AxeResult { get; }


        public AccessibilityAssertionException(AxeResult axeResult, Exception innerException)
            : base("Asserting the accessibility analysis result failed.", innerException)
        {
            AxeResult = axeResult;
        }
    }
}
