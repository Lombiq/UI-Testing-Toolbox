using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using YesSql;
using YesSql.Indexes;

namespace Lombiq.Tests.UI.Services.OrchardCoreHosting;

public sealed class FakeStore(IStore store) : IStore
{
    private readonly ConcurrentBag<ISession> _createdSessions = [];

    public IConfiguration Configuration => store.Configuration;

    public ITypeService TypeNames => store.TypeNames;

    public ISession CreateSession()
    {
        var session = store.CreateSession();
        _createdSessions.Add(session);

        return session;
    }

    public IEnumerable<IndexDescriptor> Describe(Type target, string collection = null) =>
        store.Describe(target, collection);

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

        store?.Dispose();
    }

    public Task InitializeAsync() => store.InitializeAsync();

    public Task InitializeCollectionAsync(string collection) => store.InitializeCollectionAsync(collection);

    public IStore RegisterIndexes(IEnumerable<IIndexProvider> indexProviders, string collection = null) =>
        store.RegisterIndexes(indexProviders, collection);
}
