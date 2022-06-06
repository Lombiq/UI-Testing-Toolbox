using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
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
        Func<UITestContext, Task<Stream>> action) =>
            context.FailureDumpContainer.Add(
                fileName,
                new FailureDumpItem(() => action(context)));

    /// <summary>
    /// Appends string as file content to be collected on failure dump.
    /// </summary>
    /// <param name="context"><see cref="UITestContext"/> instance.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="content">
    /// File content. Can be a composite format string <see cref="string.Format(string, object?[])"/>.
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void AppendFailureDump(
        this UITestContext context,
        string fileName,
        string content,
        params object[] args) =>
        context.FailureDumpContainer.Add(
            fileName,
            new FailureDumpItem(() => Task.FromResult(
                new MemoryStream(
                    Encoding.UTF8.GetBytes(
                        string.Format(CultureInfo.InvariantCulture, content, args))) as Stream)));

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
        Action<TContent> dispose = null) =>
        context.FailureDumpContainer.Add(fileName, new GenericFailureDumpItem<TContent>(content, getStream, dispose));

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
        Bitmap bitmap) => context
        .AppendFailureDump(fileName, bitmap, content =>
        {
            var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Bmp);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return Task.FromResult((Stream)memoryStream);
        });

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
        ImageSharpImage image) => context
        .AppendFailureDump(fileName, image, content => Task.FromResult(image.ToStream()));
}
