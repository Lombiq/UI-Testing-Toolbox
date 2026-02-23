using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Data;
using System.Threading;
using System.Threading.Tasks;
using YesSql;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[AllowAnonymous]
[DevelopmentAndLocalhostOnly]
[Route("Lombiq.Tests.UI.Shortcuts/SqlQueryMonitoringScenario")]
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

    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        var contentItemCount = await _session.QueryIndex<ContentItemIndex>().CountAsync();
        return View(model: contentItemCount);
    }

    [HttpGet("AsyncQuery")]
    public async Task<IActionResult> AsyncQuery()
    {
        var contentItemCount = await _session.QueryIndex<ContentItemIndex>().CountAsync();
        return Ok(contentItemCount);
    }

    [HttpGet("RawQuery")]
    public async Task<IActionResult> RawQuery()
    {
        var contentItemCount = await _session.RawQueryAsync<int>($"SELECT COUNT(*) FROM {nameof(ContentItemIndex)}");
        return Ok(contentItemCount);
    }

    [HttpGet("RawExecuteNonQuery")]
    public async Task<IActionResult> RawExecuteNonQuery()
    {
        var affectedRows = await _session.RawExecuteNonQueryAsync((_, prefix) =>
            $"DELETE FROM {GetContentItemIndexTableName(prefix)} WHERE 1 = 0");

        return Ok(affectedRows);
    }

    [HttpGet("CustomSessionQuery")]
    public async Task<IActionResult> CustomSessionQuery()
    {
        await using var session = _store.CreateSession();
        var contentItemCount = await session.QueryIndex<ContentItemIndex>().CountAsync();
        return Ok(contentItemCount);
    }

    [HttpGet("DirectConnectionQuery")]
    public async Task<IActionResult> DirectConnectionQuery(CancellationToken cancellationToken)
    {
        await using var connection = _dbConnectionAccessor.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // SQL uses trusted internal table metadata only.
        command.CommandText = $"SELECT COUNT(*) FROM {GetContentItemIndexTableName(_store.Configuration.TablePrefix)}";
#pragma warning restore CA2100

        var contentItemCount = await command.ExecuteScalarAsync(cancellationToken);
        return Ok(contentItemCount);
    }

    private string GetContentItemIndexTableName(string tablePrefix) =>
        _store.Configuration.SqlDialect.QuoteForTableName(
            $"{tablePrefix}{nameof(ContentItemIndex)}",
            _store.Configuration.Schema);
}
