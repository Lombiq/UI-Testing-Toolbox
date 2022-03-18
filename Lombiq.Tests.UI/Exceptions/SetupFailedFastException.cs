using System;
using System.Diagnostics.CodeAnalysis;

namespace Lombiq.Tests.UI.Exceptions;

[SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Used in a very specific case.")]
public class SetupFailedFastException : Exception
{
    public int FailureCount { get; }

    public SetupFailedFastException(int failureCount) => FailureCount = failureCount;

    public override string Message =>
         $"The given setup operation failed {FailureCount.ToTechnicalString()} times and won't be retried any " +
        $"more. All tests using this operation for setup will instantly fail.";
}
