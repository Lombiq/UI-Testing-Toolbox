using System;

namespace Lombiq.Tests.UI.Exceptions;

public class SourceInformationNotAvailableException : Exception
{
    public SourceInformationNotAvailableException(string message)
        : base(message)
    {
    }

    public SourceInformationNotAvailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SourceInformationNotAvailableException()
    {
    }
}
