using System;
using System.Globalization;

namespace Lombiq.Tests.UI.Exceptions;

// Here we only need path instead of message.
#pragma warning disable CA1032 // Implement standard exception constructors
public class VisualVerificationBaselineImageNotFoundException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
{
    public VisualVerificationBaselineImageNotFoundException(
        string path,
        int maxRetryCount,
        Exception innerException = null)
        : base(GetExceptionMessage(path, maxRetryCount), innerException)
    {
    }

    private static string GetExceptionMessage(string path, int maxRetryCount) =>
        maxRetryCount == 0 ? $"Baseline image file not found, thus it was created automatically under the path {path}."
            + " Please set its \"Build action\" to \"Embedded resource\" if you want to deploy a self-contained"
            + " (like a NuGet package) UI testing assembly. If you run the test again, this newly created verification"
            + " file will be asserted against and the assertion will pass (unless the display of the app changed in the"
            + " meantime)."
        : string.Create(
            CultureInfo.InvariantCulture,
            $"Baseline image file was not found under the path {path} and maxRetryCount is set to {maxRetryCount}, "
            + $"so it won't be generated.");
}
