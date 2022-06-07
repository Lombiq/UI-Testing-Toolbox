using System;

namespace Lombiq.Tests.UI.Exceptions;

// Here we need path instead of message
#pragma warning disable CA1032 // Implement standard exception constructors
public class VisualVerificationReferenceImageCreatedException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
{
    public VisualVerificationReferenceImageCreatedException(string path)
        : this(path, innerException: null)
    {
    }

    public VisualVerificationReferenceImageCreatedException(string path, Exception innerException)
        : base(
            $"Reference image file not found. It is created automatically on path {path}."
            + " Please set it's \"Build action\" to \"Embedded resource\" if you want to deploy a self contained UI"
            + " testing assembly.",
            innerException)
    {
    }
}
