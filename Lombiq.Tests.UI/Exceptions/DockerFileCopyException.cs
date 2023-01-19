using System;

namespace Lombiq.Tests.UI.Exceptions;

public class DockerFileCopyException : Exception
{
    public DockerFileCopyException(string message)
        : base(message)
    {
    }

    public DockerFileCopyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DockerFileCopyException()
    {
    }
}
