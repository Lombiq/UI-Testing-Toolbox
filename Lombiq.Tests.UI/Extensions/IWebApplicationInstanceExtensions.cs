using Lombiq.Tests.UI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.Environment.Shell;
using OrchardCore.Environment.Shell.Builders;
using OrchardCore.Environment.Shell.Scope;
using OrchardCore.Modules;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class IWebApplicationInstanceExtensions
{
    /// <summary>
    /// Executes a delegate using the shell scope given by <paramref name="tenant"/> in an isolated async flow, while
    /// managing the shell state and invoking tenant events.
    /// </summary>
    public static Task UsingScopeAsync(
        this IWebApplicationInstance instance,
        Func<IServiceProvider, Task> execute,
        string tenant = "Default",
        bool activateShell = true) =>
        instance.UsingScopeAsync(shellScope => execute(shellScope.ServiceProvider), tenant, activateShell);

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
        var shellHost = instance.GetRequiredService<IShellHost>();

        await (await shellHost.GetScopeAsync(tenant))
            .UsingAsync(
                shellScope =>
                {
                    // Creating a fake httpContext like in ModularBackgroundService.
                    var httpContextAccessor = shellScope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                    httpContextAccessor.HttpContext = shellScope.ShellContext.CreateHttpContext();
                    httpContextAccessor.HttpContext.Request.PathBase =
                        "/" + shellHost.GetSettings(tenant).RequestUrlPrefix ?? string.Empty;

                    return execute(shellScope);
                },
                activateShell);
    }
}

// This is from the 1.4 version of Orchard Core. During an Orchard upgrade to 1.5, remove this, and use the now public
// implementation in OrchardCore.Abstractions.
internal static class ShellExtensions
{
    public static HttpContext CreateHttpContext(this ShellContext shell)
    {
        var context = shell.Settings.CreateHttpContext();

        context.Features.Set(new ShellContextFeature
        {
            ShellContext = shell,
            OriginalPathBase = string.Empty,
            OriginalPath = "/",
        });

        return context;
    }

    public static HttpContext CreateHttpContext(this ShellSettings settings)
    {
        var context = new DefaultHttpContext().UseShellScopeServices();

        context.Request.Scheme = "https";

        var urlHost = settings.RequestUrlHost?.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

        context.Request.Host = new HostString(urlHost ?? "localhost");

        if (!string.IsNullOrWhiteSpace(settings.RequestUrlPrefix))
        {
            context.Request.PathBase = "/" + settings.RequestUrlPrefix;
        }

        context.Request.Path = "/";
        context.Items["IsBackground"] = true;

        return context;
    }
}
