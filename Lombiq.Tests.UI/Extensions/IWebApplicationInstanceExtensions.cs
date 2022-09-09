using Lombiq.Tests.UI.Services;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Scope;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class IWebApplicationInstanceExtensions
{
    /// <summary>
    /// Executes a delegate using the shell scope given by <paramref name="tenant"/> in an isolated async flow, while
    /// managing the shell state and invoking tenant events.
    /// </summary>
    public static async Task UsingScopeAsync(
        this IWebApplicationInstance instance,
        Func<ShellScope, Task> execute,
        string tenant = "Default",
        bool activateShell = true)
    {
        var shellHost = instance.GetService<IShellHost>();
        await (await shellHost.GetScopeAsync(tenant))
            .UsingAsync(execute, activateShell);
    }
}
