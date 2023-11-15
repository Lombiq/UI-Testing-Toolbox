using System;

namespace Lombiq.Tests.UI.Exceptions;

public class SecurityScanningAssertionException : Exception
{
    public SecurityScanningAssertionException(Exception innerException)
        : base(
            "Asserting the security scan result failed. Check the security scan report in the failure dump for details.",
            innerException)
    {
    }

    public SecurityScanningAssertionException()
    {
    }

    public SecurityScanningAssertionException(string message)
        : base(message)
    {
    }

    public SecurityScanningAssertionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
