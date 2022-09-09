using System;

namespace Lombiq.Tests.UI.Exceptions;

public class RecipeNotFoundException : Exception
{
    public RecipeNotFoundException(string message)
        : base(message)
    {
    }

    public RecipeNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public RecipeNotFoundException()
    {
    }
}
