using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

/// <summary>
/// Wraps <see cref="DbTransaction"/> while SQL monitoring wrappers are active. This lets monitored commands accept
/// wrapped transactions, but still pass the original provider transaction to ADO.NET when needed via <see
/// cref="Unwrap(DbTransaction)"/>.
/// </summary>
public sealed class SqlQueryMonitoringDbTransaction : DbTransaction
{
    private readonly DbTransaction _innerDbTransaction;

    protected override DbConnection DbConnection { get; }

    public SqlQueryMonitoringDbTransaction(DbTransaction dbDbTransaction, DbConnection dbConnection)
    {
        _innerDbTransaction = dbDbTransaction;
        DbConnection = dbConnection;
    }

    // If the transaction is already wrapped by SQL monitoring, return the original inner transaction.
    // This prevents double-wrapping and keeps provider transaction handling compatible.
    internal static DbTransaction Unwrap(DbTransaction dbTransaction) =>
        dbTransaction is SqlQueryMonitoringDbTransaction monitoringDbTransaction
            ? monitoringDbTransaction._innerDbTransaction
            : dbTransaction;

    public override IsolationLevel IsolationLevel => _innerDbTransaction.IsolationLevel;

    public override void Commit() => _innerDbTransaction.Commit();

    public override Task CommitAsync(CancellationToken cancellationToken = default) =>
        _innerDbTransaction.CommitAsync(cancellationToken);

    public override void Rollback() => _innerDbTransaction.Rollback();

    public override Task RollbackAsync(CancellationToken cancellationToken = default) =>
        _innerDbTransaction.RollbackAsync(cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (disposing) _innerDbTransaction.Dispose();
        base.Dispose(disposing);
    }
}
