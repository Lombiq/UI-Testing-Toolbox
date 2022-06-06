using System;

namespace Lombiq.Tests.UI.Exceptions;

public class VisualVerificationAttributeNotFoundException : Exception
{
    public VisualVerificationAttributeNotFoundException(string message)
        : base(message)
    {
    }

    public VisualVerificationAttributeNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public VisualVerificationAttributeNotFoundException()
        : this(innerException: null)
    {
    }

    public VisualVerificationAttributeNotFoundException(Exception innerException)
        : this("VisualVerificationAttribute not found", innerException)
    {
    }
}
