using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class FailureDumpUITestContextExtensions
{
    /// <summary>
    /// Appends stream as file content to be collected on failure dump.
    /// </summary>
    /// <param name="context"><see cref="UITestContext"/> instance.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="action">Gets called in failure dump collection.</param>
    /// <param name="messageIfExists">A message to display in case the desired file already exists in the dump.</param>
    public static void AppendFailureDump(
        this UITestContext context,
        string fileName,
        Func<UITestContext, Task<Stream>> action,
        string messageIfExists = null) =>
        context.AppendFailureDumpInternal(
            fileName,
            new FailureDumpItem(() => action(context)),
            messageIfExists);

    /// <summary>
    /// Appends string as file content to be collected on failure dump.
    /// </summary>
    /// <param name="context"><see cref="UITestContext"/> instance.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="content">File content.</param>
    /// <param name="messageIfExists">A message to display in case the desired file already exists in the dump.</param>
    public static void AppendFailureDump(
        this UITestContext context,
        string fileName,
        string content,
        string messageIfExists = null) =>
        context.AppendFailureDumpInternal(
            fileName,
            new FailureDumpItem(
                () => Task.FromResult(
                    new MemoryStream(
                        Encoding.UTF8.GetBytes(content)) as Stream)),
            messageIfExists);

    /// <summary>
    /// Appends generic content as file content to be collected on failure dump.
    /// </summary>
    /// <param name="context"><see cref="UITestContext"/> instance.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="content">File content.</param>
    /// <param name="getStream">Function to get a new <see cref="Stream"/> from content.</param>
    /// <param name="dispose">Action to dispose the content, if required. Can be null.</param>
    /// <param name="messageIfExists">A message to display in case the desired file already exists in the dump.</param>
    public static void AppendFailureDump<TContent>(
        this UITestContext context,
        string fileName,
        TContent content,
        Func<TContent, Task<Stream>> getStream,
        Action<TContent> dispose = null,
        string messageIfExists = null) =>
        context.AppendFailureDumpInternal(
            fileName,
            new FailureDumpItemGeneric<TContent>(content, getStream, dispose),
            messageIfExists);

    /// <summary>
    /// Appends <see cref="Image"/> as file content to be collected on failure dump.
    /// </summary>
    /// <param name="context"><see cref="UITestContext"/> instance.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="image">File content. The <see cref="Image"/> will be disposed at the end.</param>
    /// <param name="messageIfExists">A message to display in case the desired file already exists in the dump.</param>
    public static void AppendFailureDump(
        this UITestContext context,
        string fileName,
        Image image,
        string messageIfExists = null) => context
        .AppendFailureDump(
            fileName,
            image,
            _ => Task.FromResult(image.ToStream()),
            messageIfExists: messageIfExists);

    private static void AppendFailureDumpInternal(
        this UITestContext context,
        string fileName,
        IFailureDumpItem item,
        string messageIfExists = null)
    {
        if (context.FailureDumpContainer.ContainsKey(fileName))
        {
            throw new FailureDumpItemAlreadyExistsException(fileName, messageIfExists);
        }

        context.FailureDumpContainer.Add(fileName, item);
    }
}
