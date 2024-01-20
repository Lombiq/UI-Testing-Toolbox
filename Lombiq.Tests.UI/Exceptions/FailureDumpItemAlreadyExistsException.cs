using System;

namespace Lombiq.Tests.UI.Exceptions;

// Here we only need fileName and customMessage.
#pragma warning disable CA1032 // Implement standard exception constructors
public class FailureDumpItemAlreadyExistsException(string fileName, string customMessage, Exception innerException) : Exception(
        $"A failure dump item with the same file name already exists. fileName: {fileName}"
            + Environment.NewLine
            + (customMessage ?? string.Empty),
        innerException)
#pragma warning restore CA1032 // Implement standard exception constructors
{
    public FailureDumpItemAlreadyExistsException(string fileName)
        : this(fileName, customMessage: null, innerException: null)
    {
    }

    public FailureDumpItemAlreadyExistsException(string fileName, Exception innerException)
        : this(fileName, customMessage: null, innerException)
    {
    }

    public FailureDumpItemAlreadyExistsException(string fileName, string customMessage)
        : this(fileName, customMessage, innerException: null)
    {
    }
}
