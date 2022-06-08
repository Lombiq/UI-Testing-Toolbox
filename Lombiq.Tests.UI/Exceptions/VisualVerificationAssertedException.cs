using System;

namespace Lombiq.Tests.UI.Exceptions;

public class VisualVerificationAssertedException : Exception
{
    public VisualVerificationAssertedException(string message)
        : base(message)
    {
    }

    public VisualVerificationAssertedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public VisualVerificationAssertedException()
    {
    }
}
