using System;

namespace Lombiq.Tests.UI.Exceptions;

public class VisualVerificationCallerMethodNotFoundException : Exception
{
    public VisualVerificationCallerMethodNotFoundException(string message)
        : base(message)
    {
    }

    public VisualVerificationCallerMethodNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public VisualVerificationCallerMethodNotFoundException()
        : this(innerException: null)
    {
    }

    public VisualVerificationCallerMethodNotFoundException(Exception innerException)
        : this("Caller method not found", innerException)
    {
    }
}
