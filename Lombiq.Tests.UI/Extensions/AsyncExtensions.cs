using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class AsyncExtensions
{
    public static Func<T, Task> AsCompletedTask<T>(this Action<T> action) =>
        target =>
        {
            action?.Invoke(target);
            return Task.CompletedTask;
        };
}
