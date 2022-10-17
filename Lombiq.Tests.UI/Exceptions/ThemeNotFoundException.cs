using System;

namespace Lombiq.Tests.UI.Exceptions;

public class ThemeNotFoundException : Exception
{
    public ThemeNotFoundException(string message)
        : base(message)
    {
    }

    public ThemeNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ThemeNotFoundException()
    {
    }
}
