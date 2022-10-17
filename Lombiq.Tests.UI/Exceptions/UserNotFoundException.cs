using System;

namespace Lombiq.Tests.UI.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(string message)
        : base(message)
    {
    }

    public UserNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UserNotFoundException()
    {
    }
}
