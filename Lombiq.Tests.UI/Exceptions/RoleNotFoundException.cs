using System;

namespace Lombiq.Tests.UI.Exceptions;

public class RoleNotFoundException : Exception
{
    public RoleNotFoundException(string message)
        : base(message)
    {
    }

    public RoleNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public RoleNotFoundException()
    {
    }
}
