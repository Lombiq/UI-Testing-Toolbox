using System;

namespace Lombiq.Tests.UI.Exceptions;

// Here we only need fileName and customMessage.
#pragma warning disable CA1032 // Implement standard exception constructors
public class TestDumpItemAlreadyExistsException : Exception
#pragma warning restore CA1032 // Implement standard exception constructors
{
    public TestDumpItemAlreadyExistsException(string fileName)
        : this(fileName, customMessage: null, innerException: null)
    {
    }

    public TestDumpItemAlreadyExistsException(string fileName, Exception innerException)
        : this(fileName, customMessage: null, innerException)
    {
    }

    public TestDumpItemAlreadyExistsException(string fileName, string customMessage)
        : this(fileName, customMessage, innerException: null)
    {
    }

    public TestDumpItemAlreadyExistsException(string fileName, string customMessage, Exception innerException)
        : base(
            $"A test dump item with the same file name already exists. File name: {fileName}."
            + Environment.NewLine
            + (customMessage ?? string.Empty),
            innerException)
    {
    }
}
