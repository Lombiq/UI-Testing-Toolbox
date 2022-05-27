using Lombiq.Tests.UI.Services;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class FailureDumpUITestContextExtensions
{
    public static void AppendFailureDump(
        this UITestContext context,
        string fileName,
        Func<UITestContext, Task<Stream>> action) =>
            context.FailureDumpContainer.Add(
                fileName,
                () => action(context));

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
