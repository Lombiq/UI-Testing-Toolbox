using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.SqlQueryMonitoring;

public sealed class SqlQueryMonitoringDbTransaction : DbTransaction
{
    public SqlQueryMonitoringDbTransaction(DbTransaction dbTransaction, DbConnection dbConnection)
    {
        InnerTransaction = dbTransaction;
        DbConnection = dbConnection;
    }

    internal DbTransaction InnerTransaction { get; }

    internal static DbTransaction Unwrap(DbTransaction dbTransaction) =>
        dbTransaction is SqlQueryMonitoringDbTransaction monitoringDbTransaction
            ? monitoringDbTransaction.InnerTransaction
            : dbTransaction;

    public override IsolationLevel IsolationLevel => InnerTransaction.IsolationLevel;

    protected override DbConnection DbConnection { get; }

    public override void Commit() => InnerTransaction.Commit();

    public override Task CommitAsync(CancellationToken cancellationToken = default) =>
        InnerTransaction.CommitAsync(cancellationToken);

    public override void Rollback() => InnerTransaction.Rollback();

    public override Task RollbackAsync(CancellationToken cancellationToken = default) =>
        InnerTransaction.RollbackAsync(cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (disposing) InnerTransaction.Dispose();
        base.Dispose(disposing);
    }
}
