using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// We need Image class from SixLabors.ImageSharp, but there is an Image class in System.Drawing too. So lets call
// SixLabors.ImageSharp.Image as ImageSharpImage.
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace Lombiq.Tests.UI.Extensions;

public static class FailureDumpUITestContextExtensions
{
    /// <summary>
    /// Appends stream as file content to be collected on failure dump.
    /// </summary>
    /// <param name="context"><see cref="UITestContext"/> instance.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="action">Gets called in failure dump collection.</param>
    public static void AppendFailureDump(
        this UITestContext context,
        string fileName,
        Func<UITestContext, Task<Stream>> action,
        string messageIfExists = null,
        Type[] inCaseOf = null) =>
        context.AppendFailureDumpInternal(
            fileName,
            new FailureDumpItem(() => action(context), inCaseOf: inCaseOf),
            messageIfExists);

    /// <summary>
    /// Appends string as file content to be collected on failure dump.
    /// </summary>
    /// <param name="context"><see cref="UITestContext"/> instance.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="content">
    /// File content.
    /// </param>
    public static void AppendFailureDump(
        this UITestContext context,
        string fileName,
        string content,
        string messageIfExists = null,
        Type[] inCaseOf = null) =>
        context.AppendFailureDumpInternal(
            fileName,
            new FailureDumpItem(
                () => Task.FromResult(
                    new MemoryStream(
                        Encoding.UTF8.GetBytes(content)) as Stream),
                inCaseOf: inCaseOf),
            messageIfExists);

    /// <summary>
    /// Appends generic content as file content to be collected on failure dump.
    /// </summary>
    /// <param name="context"><see cref="UITestContext"/> instance.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="content">
    /// File content.
    /// </param>
    /// <param name="getStream">Function to get a new <see cref="Stream"/> from content. Can be null.</param>
    /// <param name="dispose">Action to dispose the content, if required. Can be null.</param>
    public static void AppendFailureDump<TContent>(
        this UITestContext context,
        string fileName,
        TContent content,
        Func<TContent, Task<Stream>> getStream = null,
        Action<TContent> dispose = null,
        string messageIfExists = null,
        Type[] inCaseOf = null) =>
        context.AppendFailureDumpInternal(
            fileName,
            new FailureDumpItemGeneric<TContent>(content, getStream, dispose, inCaseOf),
            messageIfExists);

    // [System.Drawing.Bitmap, System.Drawing] needed here, but System.Drawing.Bitmap is matching with
    // [System.Drawing.Bitmap, Microsoft.Data.Tools.Utilities].
#pragma warning disable CS0419 // Ambiguous reference in cref attribute
    /// <summary>
    /// Appends <see cref="System.Drawing.Bitmap"/> as file content to be collected on failure dump.
    /// </summary>
    /// <param name="context"><see cref="UITestContext"/> instance.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="bitmap">
    /// File content. The <see cref="System.Drawing.Bitmap"/> will be disposed at the end.
    /// </param>
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
    public static void AppendFailureDump(
        this UITestContext context,
        string fileName,
        Bitmap bitmap,
        string messageIfExists = null,
        Type[] inCaseOf = null) => context
        .AppendFailureDump(
            fileName,
            bitmap,
            content =>
            {
                var memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, ImageFormat.Png);
                memoryStream.Seek(0, SeekOrigin.Begin);

                return Task.FromResult((Stream)memoryStream);
            },
            messageIfExists: messageIfExists,
            inCaseOf: inCaseOf);

    /// <summary>
    /// Appends <see cref="ImageSharpImage"/> as file content to be collected on failure dump.
    /// </summary>
    /// <param name="context"><see cref="UITestContext"/> instance.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="image">
    /// File content. The <see cref="ImageSharpImage"/> will be disposed at the end.
    /// </param>
    public static void AppendFailureDump(
        this UITestContext context,
        string fileName,
        ImageSharpImage image,
        string messageIfExists = null,
        Type[] inCaseOf = null) => context
        .AppendFailureDump(
            fileName,
            image,
            content => Task.FromResult(image.ToStream()),
            messageIfExists: messageIfExists,
            inCaseOf: inCaseOf);

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
