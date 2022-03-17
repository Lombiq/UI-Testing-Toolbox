using Selenium.Axe;
using System;

namespace Lombiq.Tests.UI.Exceptions;

public class AccessibilityAssertionException : Exception
{
    public AxeResult AxeResult { get; }

    public AccessibilityAssertionException(AxeResult axeResult, bool createReportOnFailure, Exception innerException)
        : base(
            "Asserting the accessibility analysis result failed." +
              (createReportOnFailure ? " Check the accessibility report failure dump for details." : string.Empty),
            innerException) =>
        AxeResult = axeResult;

    public AccessibilityAssertionException()
    {
    }

    public AccessibilityAssertionException(string message)
        : base(message)
    {
    }

    public AccessibilityAssertionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
