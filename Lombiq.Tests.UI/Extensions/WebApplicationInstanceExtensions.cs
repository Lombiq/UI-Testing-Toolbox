using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class WebApplicationInstanceExtensions
{
    /// <summary>
    /// Asserts that the logs should be empty, i.e. contain no entries. If the assertion fails, the Shouldly exception
    /// will contain all log entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If you want to inspect the logs in a more structured way, message by message, consider using <see
    /// cref="IWebApplicationInstance.GetLogsAsync(CancellationToken)"/> or <see
    /// cref="GetLogEntriesFromAllLogsAsync(IWebApplicationInstance, CancellationToken)"/> instead. Alternatively, set
    /// log filtering options to not log unwanted messages in first place with the standard Logging:LogLevel app
    /// configuration (see the samples).
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the log retrieval.</param>
    public static async Task LogsShouldBeEmptyAsync(
        this IWebApplicationInstance webApplicationInstance,
        CancellationToken cancellationToken = default)
    {
        var logs = (await webApplicationInstance.GetLogsAsync(cancellationToken)).AsList();
        logs.ShouldNotContain(log => log.EntryCount > 0, await logs.ToFormattedStringAsync());
    }

    /// <summary>
    /// Asserts that the logs should contain any entry matching the given predicate. If the assertion fails, the
    /// Shouldly exception will contain all log entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If you want to inspect the logs in a more structured way, message by message, consider using <see
    /// cref="IWebApplicationInstance.GetLogsAsync(CancellationToken)"/> or <see
    /// cref="GetLogEntriesFromAllLogsAsync(IWebApplicationInstance, CancellationToken)"/> instead. Alternatively, set
    /// log filtering options to not log unwanted messages in first place with the standard Logging:LogLevel app
    /// configuration (see the samples).
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the log retrieval.</param>
    /// <param name="logEntryPredicate">
    /// A predicate that when returns <see langword="true"/>, the assertion will fail.
    /// </param>
    public static Task LogsShouldContainAsync(
        this IWebApplicationInstance webApplicationInstance,
        Expression<Func<IApplicationLogEntry, bool>> logEntryPredicate,
        CancellationToken cancellationToken = default) =>
        AssertLogsAsync(webApplicationInstance, logEntryPredicate, ShouldBeEnumerableTestExtensions.ShouldContain, cancellationToken);

    /// <summary>
    /// Asserts that the logs should NOT contain any entries with <see cref="LogLevel.Error"/> and above. If the
    /// assertion fails, the Shouldly exception will contain all log entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If you want to inspect the logs in a more structured way, message by message, consider using <see
    /// cref="IWebApplicationInstance.GetLogsAsync(CancellationToken)"/> or <see
    /// cref="GetLogEntriesFromAllLogsAsync(IWebApplicationInstance, CancellationToken)"/> instead. Alternatively, set
    /// log filtering options to not log unwanted messages in first place with the standard Logging:LogLevel app
    /// configuration (see the samples).
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the log retrieval.</param>
    public static Task LogsShouldNotContainErrorsAsync(
        this IWebApplicationInstance webApplicationInstance,
        CancellationToken cancellationToken = default) =>
        AssertLogsAsync(
            webApplicationInstance,
            logEntry => logEntry.Level > LogLevel.Error,
            ShouldBeEnumerableTestExtensions.ShouldNotContain,
            cancellationToken);

    /// <summary>
    /// Asserts that the logs should NOT contain any entries matching the given predicate. If the assertion fails, the
    /// Shouldly exception will contain all log entries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If you want to inspect the logs in a more structured way, message by message, consider using <see
    /// cref="IWebApplicationInstance.GetLogsAsync(CancellationToken)"/> or <see
    /// cref="GetLogEntriesFromAllLogsAsync(IWebApplicationInstance, CancellationToken)"/> instead. Alternatively, set
    /// log filtering options to not log unwanted messages in first place with the standard Logging:LogLevel app
    /// configuration (see the samples).
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the log retrieval.</param>
    /// <param name="logEntryPredicate">
    /// A predicate that when returns <see langword="true"/>, the assertion will fail.
    /// </param>
    public static Task LogsShouldNotContainAsync(
        this IWebApplicationInstance webApplicationInstance,
        Expression<Func<IApplicationLogEntry, bool>> logEntryPredicate,
        CancellationToken cancellationToken = default) =>
        AssertLogsAsync(webApplicationInstance, logEntryPredicate, ShouldBeEnumerableTestExtensions.ShouldNotContain, cancellationToken);

    /// <summary>
    /// Retrieves all the logs and concatenates them into a single formatted string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If you want to inspect the logs in a more structured way, message by message, consider using <see
    /// cref="IWebApplicationInstance.GetLogsAsync(CancellationToken)"/> or <see
    /// cref="GetLogEntriesFromAllLogsAsync(IWebApplicationInstance, CancellationToken)"/> instead. Alternatively, set
    /// log filtering options to not log unwanted messages in first place with the standard Logging:LogLevel app
    /// configuration (see the samples).
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the log retrieval.</param>
    public static async Task<string> GetLogContentsAsync(
        this IWebApplicationInstance webApplicationInstance,
        CancellationToken cancellationToken = default) =>
        await (await webApplicationInstance.GetLogsAsync(cancellationToken)).ToFormattedStringAsync();

    /// <summary>
    /// Retrieves the log entries from all application logs.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can cancel the log retrieval.</param>
    public static async Task<IEnumerable<IApplicationLogEntry>> GetLogEntriesFromAllLogsAsync(
        this IWebApplicationInstance webApplicationInstance,
        CancellationToken cancellationToken = default)
    {
        var logs = await webApplicationInstance.GetLogsAsync(cancellationToken);
        var logEntries = new List<IApplicationLogEntry>();

        foreach (var log in logs)
        {
            logEntries.AddRange(await log.GetEntriesAsync());
        }

        return logEntries;
    }

    /// <summary>
    /// Get service of type <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The type of service service to get.</typeparam>
    /// <returns>An instance of the service of type <typeparamref name="TService"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// There is no service of type <typeparamref name="TService"/>.
    /// </exception>
    public static TService GetRequiredService<TService>(this IWebApplicationInstance webApplicationInstance) =>
        webApplicationInstance.Services.GetRequiredService<TService>();

    /// <summary>
    /// Restarts, i.e. pauses and resumes, the web application instance.
    /// </summary>
    public static async Task RestartAsync(this IWebApplicationInstance webApplicationInstance)
    {
        await webApplicationInstance.PauseAsync();
        await webApplicationInstance.ResumeAsync();
    }

    private static async Task AssertLogsAsync(
        IWebApplicationInstance webApplicationInstance,
        Expression<Func<IApplicationLogEntry, bool>> logEntryPredicate,
        Action<IEnumerable<IApplicationLogEntry>, Expression<Func<IApplicationLogEntry, bool>>, string> shouldlyMethod,
        CancellationToken cancellationToken = default)
    {
        var logs = await (await webApplicationInstance.GetLogsAsync(cancellationToken))
            .AwaitEachAsync(CachedApplicationLog.FromLogAsync);

        var logContents = logs.ToFormattedStringCached();

        foreach (var log in logs)
        {
            shouldlyMethod(await log.GetEntriesAsync(), logEntryPredicate, logContents);
        }
    }
}
