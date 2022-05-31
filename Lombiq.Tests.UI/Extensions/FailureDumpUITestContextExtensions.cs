using Lombiq.Tests.UI.Services;
using System;
using System.Globalization;
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
    public static void AppendFailureDump(
        this UITestContext context,
        string fileName,
        Func<UITestContext, Task<Stream>> action) =>
            context.FailureDumpContainer.Add(
                fileName,
                () => action(context));

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
            () => Task.FromResult(
                new MemoryStream(
                    Encoding.UTF8.GetBytes(
                        string.Format(CultureInfo.InvariantCulture, content, args))) as Stream));
}
