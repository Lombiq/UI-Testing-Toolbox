using System;

namespace Lombiq.Tests.UI.Exceptions;

public class CreateUserFailedException : Exception
{
    public CreateUserFailedException(string message)
        : base(message)
    {
    }

    public CreateUserFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public CreateUserFailedException()
    {
    }
}
