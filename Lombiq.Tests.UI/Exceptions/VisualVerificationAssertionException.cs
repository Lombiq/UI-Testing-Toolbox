using System;

namespace Lombiq.Tests.UI.Exceptions;

public class VisualVerificationAssertionException : Exception
{
    public VisualVerificationAssertionException(string message)
        : base(message)
    {
    }

    public VisualVerificationAssertionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public VisualVerificationAssertionException()
    {
    }
}
