using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql;
using YesSql.Indexes;

namespace Lombiq.Tests.UI.Services.OrchardCoreHosting;

public sealed class FakeStore : IStore
{
    private readonly List<ISession> _createdSessions = new();
    private readonly IStore _store;

    public FakeStore(IStore store) =>
        _store = store;

    public IConfiguration Configuration => _store.Configuration;

    public ITypeService TypeNames => _store.TypeNames;

    public ISession CreateSession()
    {
        lock (_createdSessions)
        {
            var session = _store.CreateSession();
            _createdSessions.Add(session);

            return session;
        }
    }

    public IEnumerable<IndexDescriptor> Describe(Type target, string collection = null) => _store.Describe(target, collection);

    public void Dispose()
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
            }
#pragma warning restore S108 // Nested blocks of code should not be left empty
        }

        _createdSessions.Clear();

        _store?.Dispose();
    }

    public Task InitializeAsync() => _store.InitializeAsync();

    public Task InitializeCollectionAsync(string collection) => _store.InitializeCollectionAsync(collection);

    public IStore RegisterIndexes(IEnumerable<IIndexProvider> indexProviders, string collection = null) =>
        _store.RegisterIndexes(indexProviders, collection);
}
