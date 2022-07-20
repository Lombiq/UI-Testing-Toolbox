using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YesSql;

namespace Lombiq.Tests.UI.Services.OrchardCoreHosting;

// Taken from: https://github.com/benday-inc/SeleniumDemo.
public class OrchardApplicationFactory<TStartup> : WebApplicationFactory<TStartup>
   where TStartup : class
{
    private readonly Action<IWebHostBuilder> _configuration;
    private readonly List<IStore> _createdStores = new();
    private IHost _host;

    public OrchardApplicationFactory(string rootUrl, Action<IWebHostBuilder> configuration = null)
    {
        RootUrl = rootUrl;
        _configuration = configuration;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(ConfigureTestServices);
        _configuration?.Invoke(builder);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();

        // configure and start the actual host.
        builder.ConfigureWebHost(webHostBuilder =>
            webHostBuilder.UseKestrel()
                .UseUrls(RootUrl)
                .ConfigureTestServices(ConfigureTestServices));

        _host = builder.Build();
        _host.Start();

        return host;
    }

    private void ConfigureTestServices(IServiceCollection services)
    {
        var builder = services
                .LastOrDefault(d => d.ServiceType == typeof(OrchardCoreBuilder))?
                .ImplementationInstance as OrchardCoreBuilder;
        builder.ConfigureServices(builderServices => AddFakeStore(builderServices));
    }

    private void AddFakeStore(IServiceCollection services)
    {
        var storeDescriptor = services.LastOrDefault(d => d.ServiceType == typeof(IStore));

        services.RemoveAll<IStore>();

        services.AddSingleton<IStore>(serviceProvider =>
        {
            var store = (IStore)storeDescriptor.ImplementationFactory.Invoke(serviceProvider);
            if (store is null)
            {
                return null;
            }

            lock (_createdStores)
            {
                var fakeStore = new FakeStore((IStore)storeDescriptor.ImplementationFactory.Invoke(serviceProvider));
                _createdStores.Add(fakeStore);

                return fakeStore;
            }
        });
    }

    // WebApplicationFactory<>.DisposeAsync() calls "GC.SuppressFinalize".
#pragma warning disable CA1816 // Call GC.SuppressFinalize correctly
    public override async ValueTask DisposeAsync()
#pragma warning restore CA1816 // Call GC.SuppressFinalize correctly
    {
        await _host?.StopAsync();

        foreach (var store in _createdStores)
        {
            store.Dispose();
        }

        _createdStores.Clear();

        _host?.Dispose();
        _host = null;

        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
    }

    public string RootUrl { get; private set; }
}
