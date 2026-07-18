using Deque.AxeCore.Commons;
using Lombiq.Tests.UI.Models;
using System;

namespace Lombiq.Tests.UI.Exceptions;

public class AccessibilityAssertionException : Exception, IAssertionException
{
    public AxeResult AxeResult { get; }
    public AccessibilityCheckingResult Result { get; }

    public AccessibilityAssertionException(AxeResult axeResult, bool createReportOnFailure, Exception innerException)
        : this((AccessibilityCheckingResult)axeResult, createReportOnFailure, innerException) =>
        AxeResult = axeResult;

    public AccessibilityAssertionException(AccessibilityCheckingResult result, bool createReportOnFailure, Exception innerException)
        : base(
            "Asserting the accessibility analysis result failed." +
              (createReportOnFailure ? " Check the accessibility report failure dump for details." : string.Empty),
            innerException) =>
        Result = result;

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
