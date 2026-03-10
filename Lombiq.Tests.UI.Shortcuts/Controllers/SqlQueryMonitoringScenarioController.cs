using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Data;
using System.Threading.Tasks;
using YesSql;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

/// <summary>
/// Test-only endpoints used by SQL monitoring UI tests to exercise different query execution paths.
/// </summary>
[AllowAnonymous]
[DevelopmentAndLocalhostOnly]
public sealed class SqlQueryMonitoringScenarioController : Controller
{
    private readonly ISession _session;
    private readonly IStore _store;
    private readonly IDbConnectionAccessor _dbConnectionAccessor;

    public SqlQueryMonitoringScenarioController(
        ISession session,
        IStore store,
        IDbConnectionAccessor dbConnectionAccessor)
    {
        _session = session;
        _store = store;
        _dbConnectionAccessor = dbConnectionAccessor;
    }

    /// <summary>
    /// Renders a page that executes a standard YesSql query so page-change SQL monitoring assertions can run on HTML.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var contentItemCount = await _session.QueryIndex<ContentItemIndex>().CountAsync();
        return View(model: contentItemCount);
    }

    /// <summary>
    /// Executes the same YesSql query as <see cref="Index"/> but returns JSON to simulate a follow-up async request.
    /// </summary>
    public async Task<IActionResult> AsyncQuery()
    {
        var contentItemCount = await _session.QueryIndex<ContentItemIndex>().CountAsync();
        return Ok(contentItemCount);
    }

    /// <summary>
    /// Returns a response without executing SQL, used to verify that noisy non-SQL requests do not evict actionable
    /// SQL summaries from the monitoring store.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult NoSql() => Ok("No SQL executed.");

    /// <summary>
    /// Executes a raw SQL read query to verify RawQueryAsync is captured by SQL monitoring.
    /// </summary>
    public async Task<IActionResult> RawQuery()
    {
        var contentItemCount = await _session.RawQueryAsync<int>(
            $"SELECT COUNT(*) FROM {GetContentItemIndexTableName(_store.Configuration.TablePrefix)}");
        return Ok(contentItemCount);
    }

    /// <summary>
    /// Executes a raw SQL write command.
    /// </summary>
    public async Task<IActionResult> RawExecuteNonQuery()
    {
        var affectedRows = await _session.RawExecuteNonQueryAsync((_, prefix) =>
            $"DELETE FROM {GetContentItemIndexTableName(prefix)} WHERE 1 = 0");

        return Ok(affectedRows);
    }

    /// <summary>
    /// Executes a YesSql query from a manually created session to verify custom-session instrumentation.
    /// </summary>
    public async Task<IActionResult> CustomSessionQuery()
    {
        await using var session = _store.CreateSession();
        var contentItemCount = await session.QueryIndex<ContentItemIndex>().CountAsync();
        return Ok(contentItemCount);
    }

    /// <summary>
    /// Executes a direct ADO.NET query through <see cref="IDbConnectionAccessor"/> to cover low-level SQL access.
    /// </summary>
    public async Task<IActionResult> DirectConnectionQuery()
    {
        await using var connection = _dbConnectionAccessor.CreateConnection();
        await connection.OpenAsync(HttpContext.RequestAborted);

        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // SQL uses trusted internal table metadata only.
        command.CommandText = $"SELECT COUNT(*) FROM {GetContentItemIndexTableName(_store.Configuration.TablePrefix)}";
#pragma warning restore CA2100

        var contentItemCount = await command.ExecuteScalarAsync(HttpContext.RequestAborted);
        return Ok(contentItemCount);
    }

    private string GetContentItemIndexTableName(string tablePrefix) =>
        _store.Configuration.SqlDialect.QuoteForTableName(
            $"{tablePrefix}{nameof(ContentItemIndex)}",
            _store.Configuration.Schema);
}
