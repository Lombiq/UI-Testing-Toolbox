using Lombiq.Tests.UI.Services.Counters.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using YesSql;
using YesSql.Indexes;

namespace Lombiq.Tests.UI.Services.Counters;

public sealed class SessionProbe : CounterProbe, IOutOfTestContextCounterProbe, ISession, IRelativeUrlConfigurationKey
{
    private readonly ISession _session;
    public string RequestMethod { get; init; }
    public Uri AbsoluteUri { get; init; }
    DbTransaction ISession.CurrentTransaction => _session.CurrentTransaction;
    IStore ISession.Store => _session.Store;

    public SessionProbe(ICounterDataCollector counterDataCollector, string requestMethod, Uri absoluteUri, ISession session)
        : base(counterDataCollector)
    {
        RequestMethod = requestMethod;
        AbsoluteUri = absoluteUri;
        _session = session;
    }

    public override string DumpHeadline() => $"{nameof(SessionProbe)}, [{RequestMethod}]{AbsoluteUri}";

    protected override void OnDisposing() => _session.Dispose();

    // Needs to be implemented on mock class.
#pragma warning disable CS0618 // Type or member is obsolete
    void ISession.Save(object obj, bool checkConcurrency, string collection) =>
        _session.Save(obj, checkConcurrency, collection);
#pragma warning restore CS0618 // Type or member is obsolete
    Task ISession.SaveAsync(object obj, bool checkConcurrency, string collection) =>
        _session.SaveAsync(obj, checkConcurrency, collection);
    void ISession.Delete(object item, string collection) =>
        _session.Delete(item, collection);
    bool ISession.Import(object item, long id, long version, string collection) =>
        _session.Import(item, id, version, collection);
    void ISession.Detach(object item, string collection) =>
        _session.Detach(item, collection);
    Task<IEnumerable<T>> ISession.GetAsync<T>(long[] ids, string collection) =>
        _session.GetAsync<T>(ids, collection);
    IQuery ISession.Query(string collection) =>
        _session.Query(collection);
    IQuery<T> ISession.ExecuteQuery<T>(ICompiledQuery<T> compiledQuery, string collection) =>
        _session.ExecuteQuery(compiledQuery, collection);
    Task ISession.CancelAsync() => _session.CancelAsync();
    Task ISession.FlushAsync() => _session.FlushAsync();
    Task ISession.SaveChangesAsync() => _session.SaveChangesAsync();
    Task<DbConnection> ISession.CreateConnectionAsync() => _session.CreateConnectionAsync();
    Task<DbTransaction> ISession.BeginTransactionAsync() => _session.BeginTransactionAsync();
    Task<DbTransaction> ISession.BeginTransactionAsync(IsolationLevel isolationLevel) =>
        _session.BeginTransactionAsync(isolationLevel);
    ISession ISession.RegisterIndexes(IIndexProvider[] indexProviders, string collection) =>
        _session.RegisterIndexes(indexProviders, collection);
    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await _session.DisposeAsync();
        // Should be at the end because the Session implementation calls CommitOrRollbackTransactionAsync in
        // DisposeAsync and we should count the executed DB commands in it.
        Dispose();
    }

    #region IRelativeUrlConfigurationKey implementation

    Uri IRelativeUrlConfigurationKey.Url => AbsoluteUri;
    bool IRelativeUrlConfigurationKey.ExactMatch => false;
    bool IEquatable<ICounterConfigurationKey>.Equals(ICounterConfigurationKey other) =>
        this.EqualsWith(other as IRelativeUrlConfigurationKey);

    #endregion
}
