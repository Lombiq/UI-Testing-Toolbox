using System;

namespace Lombiq.Tests.UI.Exceptions;

// Here we only need path instead of message.
#pragma warning disable CA1032 // Implement standard exception constructors
public class VisualVerificationBaselineImageNotFoundException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
{
    public VisualVerificationBaselineImageNotFoundException(
        string path,
        bool isFinalTry,
        Exception innerException = null)
        : base(GetExceptionMessage(path, isFinalTry), innerException)
    {
    }

    private static string GetExceptionMessage(string path, bool isFinalTry) =>
        isFinalTry
            ? $"Baseline image file not found, thus it was created automatically under the path {path}. Please set " +
              $"its \"Build action\" to \"Embedded resource\" if you want to deploy a self-contained (like a NuGet " +
              $"package) UI testing assembly. If you run the test again, this newly created verification file will " +
              $"be asserted against and the assertion will pass (unless the display of the app changed in the " +
              $"meantime)."
            : $"Baseline image file was not found under the path {path} and this isn't the last try.";
}
