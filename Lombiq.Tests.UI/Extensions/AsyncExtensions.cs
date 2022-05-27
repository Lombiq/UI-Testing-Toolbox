using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class AsyncExtensions
{
    [SuppressMessage(
        "Minor Code Smell",
        "S4261:Methods should be named according to their synchronicities",
        Justification =
            "This is a conversion method, and we want to make it explicit that the result has an async signature.")]
    public static Func<T, Task> ToAsync<T>(this Action<T> action) =>
        target =>
        {
            action?.Invoke(target);
            return Task.CompletedTask;
        };
}
