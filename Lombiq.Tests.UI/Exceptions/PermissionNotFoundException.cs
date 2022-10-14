using System;

namespace Lombiq.Tests.UI.Exceptions;

public class PermissionNotFoundException : Exception
{
    public PermissionNotFoundException(string message)
        : base(message)
    {
    }

    public PermissionNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public PermissionNotFoundException()
    {
    }
}
