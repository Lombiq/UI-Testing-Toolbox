using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class WebApplicationInstanceExtensions
{
    /// <summary>
    /// Asserting that the logs should be empty. When they aren't the Shouldly exception will contain the logs'
    /// contents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If you want to inspect the logs in a more structured way, message by message, consider using <see
    /// cref="IWebApplicationInstance.GetLogsAsync(CancellationToken)"/> directly instead.
    /// </para>
    /// </remarks>
    public static async Task LogsShouldBeEmptyAsync(
        this IWebApplicationInstance webApplicationInstance,
        CancellationToken cancellationToken = default)
    {
        var logs = await webApplicationInstance.GetLogsAsync(cancellationToken);
        logs.ShouldNotContain(log => log.MessageCount > 0, await logs.ToFormattedStringAsync());
    }

    public static async Task LogsShouldNotContainAsync(
        this IWebApplicationInstance webApplicationInstance,
        Expression<Func<IApplicationLogEntry, bool>> logEntryPredicate,
        CancellationToken cancellationToken = default)
    {
        var logs = await webApplicationInstance.GetLogsAsync(cancellationToken);

        foreach (var log in logs)
        {
            (await log.GetEntriesAsync()).ShouldNotContain(logEntryPredicate, await logs.ToFormattedStringAsync());
        }
    }

    /// <summary>
    /// Retrieves all the logs and concatenates them into a single formatted string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If you want to inspect the logs in a more structured way, message by message, consider using <see
    /// cref="IWebApplicationInstance.GetLogsAsync(CancellationToken)"/> directly instead.
    /// </para>
    /// </remarks>
    public static async Task<string> GetLogOutputAsync(
        this IWebApplicationInstance webApplicationInstance,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default) cancellationToken = CancellationToken.None;

        return await (await webApplicationInstance.GetLogsAsync(cancellationToken)).ToFormattedStringAsync();
    }

    /// <summary>
    /// Get service of type <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The type of service object to get.</typeparam>
    /// <returns>A service object of type <typeparamref name="TService"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// There is no service of type <typeparamref name="TService"/>.
    /// </exception>
    public static TService GetRequiredService<TService>(this IWebApplicationInstance webApplicationInstance) =>
        webApplicationInstance.Services.GetRequiredService<TService>();
}
