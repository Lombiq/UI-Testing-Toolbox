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
    private readonly List<IStore> _createdStores = [];

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
        builder.ConfigureHostConfiguration(configurationBuilder => _configureHost?.Invoke(configurationBuilder));
        // This lock is to avoid parallel start of the application.
        // Microsoft.Extensions.Hosting.HostFactoryResolver.HostingListener.CreateHost() starts a new thread for the web
        // application instance which can cause issues in e.g.:
        // NLog.Config.Factory<TBaseType, TAttributeType>.RegisterDefinition() which is using non-thread-safe Dictionary
        // to store cached types when initializing the default logger instance.
        lock (OrchardApplicationFactoryCounter.CreateHostLock)
        {
            // Moving host startup out of the xUnit synchronization context to a new thread, to avoid potential
            // deadlocks and thus dotnet test getting randomly stuck due to sync-over-async code in
            // WebApplicationFactory. See ASP.NET Core issue: https://github.com/dotnet/aspnetcore/issues/43353. See our
            // issue for more details about the whole topic: https://github.com/Lombiq/UI-Testing-Toolbox/issues/228.
            // Solution taken from:
            // https://www.strathweb.com/2021/05/the-curious-case-of-asp-net-core-integration-test-deadlock/.

            // The original CreateHost() is just the following:
            ////var host = builder.Build();
            ////host.Start();
            ////return host;
            // See https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.Testing/src/WebApplicationFactory.cs for
            // the latest source.

            var host = builder.Build();
            Task.Run(() => host.StartAsync()).GetAwaiter().GetResult();
            return host;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(ConfigureTestServices)
            .ConfigureLogging((context, loggingBuilder) =>
            {
                var environment = context.HostingEnvironment;
                var nLogConfig = Path.Combine(environment.ContentRootPath, "NLog.config");
                var factory = new LogFactory()
                    .Setup()
                    .LoadConfigurationFromFile(nLogConfig)
                    .LogFactory;

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

        builder.ConfigureServices(
            builderServices =>
            {
                AddFakeStore(builderServices);
                AddFakeViewCompilerProvider(builderServices);
            },
            int.MaxValue);
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

    // This is required because OrchardCore adds OrchardCore.Mvc.SharedViewCompilerProvider as IViewCompilerProvider but
    // it holds a IViewCompiler(Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation.RuntimeViewCompiler) instance
    // reference in a static member(_compiler) and it not get released on IHost.StopAsync() call, and this cause an
    // ObjectDisposedException on next run.
    private static void AddFakeViewCompilerProvider(IServiceCollection services) =>
        services.AddSingleton<IViewCompilerProvider, FakeViewCompilerProvider>();

    public override async ValueTask DisposeAsync()
    {
        foreach (var store in _createdStores)
        {
            store.Dispose();
        }

        _createdStores.Clear();

        try
        {
            await base.DisposeAsync();
        }
        catch (NullReferenceException)
        {
            // The base DisposeAsync() randomly throws an NRE when tests are concurrently executed locally. This doesn't
            // seem to be a problem, though.
        }

        SqliteConnection.ClearAllPools();
    }
}

internal static class OrchardApplicationFactoryCounter
{
    public static object CreateHostLock { get; } = new();
}
