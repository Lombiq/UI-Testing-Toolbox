using Lombiq.Tests.Integration.Services;
using Lombiq.Tests.UI.Services.Counters;
using Lombiq.Tests.UI.Services.Counters.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YesSql;
using ISession = YesSql.ISession;

namespace Lombiq.Tests.UI.Services.OrchardCoreHosting;

public sealed class OrchardApplicationFactory<TStartup> : WebApplicationFactory<TStartup>, IProxyConnectionProvider
   where TStartup : class
{
    private readonly ICounterDataCollector _counterDataCollector;
    private readonly Action<IConfigurationBuilder> _configureHost;
    private readonly Action<IWebHostBuilder> _configuration;
    private readonly Action<ConfigurationManager, OrchardCoreBuilder> _configureOrchard;
    private readonly ConcurrentBag<IStore> _createdStores = [];
    private readonly CancellationToken _cancellationToken;

    public OrchardApplicationFactory(
        ICounterDataCollector counterDataCollector,
        Action<IConfigurationBuilder> configureHost,
        Action<IWebHostBuilder> configuration,
        Action<ConfigurationManager, OrchardCoreBuilder> configureOrchard,
        CancellationToken cancellationToken)
    {
        _counterDataCollector = counterDataCollector;
        _configureHost = configureHost;
        _configuration = configuration;
        _configureOrchard = configureOrchard;
        _cancellationToken = cancellationToken;
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
            Task.Run(() => host.StartAsync(_cancellationToken), _cancellationToken).GetAwaiter().GetResult();
            return host;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .ConfigureTestServices(ConfigureTestServices)
            // NLog, if used, will put log files into configDir. Not setting this would use the default, which would be
            // App_Data/App_Data/logs.
            .ConfigureLogging((context, _) => LogManager.Configuration.Variables["configDir"] = context.HostingEnvironment.ContentRootPath);

        _configuration?.Invoke(builder);
    }

    private void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton(_counterDataCollector);

        var builder = services
            .LastOrDefault(descriptor => descriptor.ServiceType == typeof(OrchardCoreBuilder))?
            .ImplementationInstance as OrchardCoreBuilder
            ?? throw new InvalidOperationException(
                "Please call WebApplicationBuilder.Services.AddOrchardCms() in your Program.cs.");
        var configuration = services
            .LastOrDefault(descriptor => descriptor.ServiceType == typeof(ConfigurationManager))?
            .ImplementationInstance as ConfigurationManager
            ?? throw new InvalidOperationException(
                $"Please register the {nameof(ConfigurationManager)} instance in the Service Collection in your " +
                "Program.cs, following the documentation.");

        _configureOrchard?.Invoke(configuration, builder);

        builder.ConfigureServices(
            builderServices =>
            {
                AddFakeStore(builderServices);
                AddFakeViewCompilerProvider(builderServices);
                AddSessionProbe(builderServices);
            },
            int.MaxValue);

        builder.Configure(
            app => app.UseMiddleware<RequestProbeMiddleware>(),
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

            store.Configuration.ConnectionFactory = new ProbedConnectionFactory(
                store.Configuration.ConnectionFactory,
                _counterDataCollector);

            var fakeStore = new FakeStore(store);
            _createdStores.Add(fakeStore);

            return fakeStore;
        });
    }

    private void AddSessionProbe(IServiceCollection services)
    {
        var sessionDescriptor = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(ISession));

        services.RemoveAll<ISession>();

        services.AddScoped<ISession>(serviceProvider =>
        {
            var session = (ISession)sessionDescriptor.ImplementationFactory.Invoke(serviceProvider);
            if (session is null)
            {
                return null;
            }

            var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

            // The actual HttpContext can be null during the IShellHost.InitializeAsync() when creating a new scope.
            // E.g.: UsingScopeWebApplicationInstanceExtensions.UsingScopeAsync.
            // We have to handle this situation here.
            var requestMethod = httpContextAccessor.HttpContext?.Request?.Method ?? "UNKNOWN";
            var requestUrl = httpContextAccessor?.HttpContext?.Request?.GetEncodedUrl() ?? "https://localhost/unknown";

            return new SessionProbe(
                _counterDataCollector,
                requestMethod,
                new Uri(requestUrl),
                session);
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
