using Lombiq.Tests.Integration.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YesSql;

namespace Lombiq.Tests.UI.Services.OrchardCoreHosting;

public sealed class OrchardApplicationFactory<TStartup> : WebApplicationFactory<TStartup>, IProxyConnectionProvider
   where TStartup : class
{
    private readonly Action<IConfigurationBuilder> _configureHost;
    private readonly Action<IWebHostBuilder> _configuration;
    private readonly Action<ConfigurationManager, OrchardCoreBuilder> _configureOrchard;
    private readonly List<IStore> _createdStores = new();

    public OrchardApplicationFactory(
        Action<IConfigurationBuilder> configureHost = null,
        Action<IWebHostBuilder> configuration = null,
        Action<ConfigurationManager, OrchardCoreBuilder> configureOrchard = null)
    {
        _configureHost = configureHost;
        _configuration = configuration;
        _configureOrchard = configureOrchard;
    }

    public Uri BaseAddress => ClientOptions.BaseAddress;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configurationBuilder =>
            _configureHost?.Invoke(configurationBuilder));

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(ConfigureTestServices)
            .ConfigureLogging((context, loggingBuilder) =>
            {
                var environment = context.HostingEnvironment;
                var nLogConfig = Path.Combine(environment.ContentRootPath, "NLog.config");
                var factory = new LogFactory()
                    .LoadConfiguration(nLogConfig);

                factory.Configuration.Variables["configDir"] = environment.ContentRootPath;

                loggingBuilder.AddNLogWeb(factory, new NLogAspNetCoreOptions { ReplaceLoggerFactory = true });
            });

        _configuration?.Invoke(builder);
    }

    private void ConfigureTestServices(IServiceCollection services)
    {
        var builder = services
                .LastOrDefault(descriptor => descriptor.ServiceType == typeof(OrchardCoreBuilder))?
                .ImplementationInstance as OrchardCoreBuilder
                ?? throw new InvalidOperationException(
                    "Please call WebApplicationBuilder.Services.AddOrchardCms() in your Program.cs!");
        var configuration = services
                .LastOrDefault(descriptor => descriptor.ServiceType == typeof(ConfigurationManager))?
                .ImplementationInstance as ConfigurationManager
                ?? throw new InvalidOperationException(
                    $"Please add {nameof(ConfigurationManager)} instance to WebApplicationBuilder.Services in your Program.cs!");

        _configureOrchard?.Invoke(configuration, builder);

        builder.ConfigureServices(builderServices =>
        {
            AddFakeStore(builderServices);
            AddFakeViewCompilerProvider(builderServices);
        });
    }

    private void AddFakeStore(IServiceCollection services)
    {
        var storeDescriptor = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IStore));

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

    // This is required because OrchardCore adds OrchardCore.Mvc.SharedViewCompilerProvider as IViewCompilerProvider but it
    // holds a IViewCompiler(Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation.RuntimeViewCompiler) instance reference in
    // a static member(_compiler) and it not get released on IHost.StopAsync() call, and this cause an ObjectDisposedException
    // on next run.
    private static void AddFakeViewCompilerProvider(IServiceCollection services) =>
        services.AddSingleton<IViewCompilerProvider, FakeViewCompilerProvider>();

    public override async ValueTask DisposeAsync()
    {
        foreach (var store in _createdStores)
        {
            store.Dispose();
        }

        _createdStores.Clear();

        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
    }
}
