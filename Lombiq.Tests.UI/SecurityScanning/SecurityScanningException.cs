using System;

namespace Lombiq.Tests.UI.SecurityScanning;

public class SecurityScanningException : Exception
{
    public SecurityScanningException()
    {
    }

    public SecurityScanningException(string message)
        : base(message)
    {
    }

    public SecurityScanningException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
