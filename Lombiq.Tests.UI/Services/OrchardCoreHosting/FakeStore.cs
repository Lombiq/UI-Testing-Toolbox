using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql;
using YesSql.Indexes;

namespace Lombiq.Tests.UI.Services.OrchardCoreHosting;

public sealed class FakeStore : IStore
{
    private readonly ConcurrentBag<ISession> _createdSessions = [];
    private readonly IStore _store;
    private bool _isDisposed;

    public FakeStore(IStore store) => _store = store;

    public IConfiguration Configuration => _store.Configuration;

    public ITypeService TypeNames => _store.TypeNames;

    public ISession CreateSession(bool withTracking = true)
    {
        var session = _store.CreateSession();
        _createdSessions.Add(session);

        return session;
    }

    public IEnumerable<IndexDescriptor> Describe(Type target, string collection = null) =>
        _store.Describe(target, collection);

    public Task InitializeAsync() => _store.InitializeAsync();

    public Task InitializeCollectionAsync(string collection) => _store.InitializeCollectionAsync(collection);

    public IStore RegisterIndexes(IEnumerable<IIndexProvider> indexProviders, string collection = null) =>
        _store.RegisterIndexes(indexProviders, collection);

    private void Dispose(bool disposing)
    {
        if (!_isDisposed && disposing)
        {
            foreach (var session in _createdSessions)
            {
                try
                {
                    session.Dispose();
                }
                catch
#pragma warning disable S108 // Nested blocks of code should not be left empty
                {
                    // The mocked session can cause exception, but we can't do anything with it here.
                }
#pragma warning restore S108 // Nested blocks of code should not be left empty
            }

            _createdSessions.Clear();

            _store?.Dispose();

            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
