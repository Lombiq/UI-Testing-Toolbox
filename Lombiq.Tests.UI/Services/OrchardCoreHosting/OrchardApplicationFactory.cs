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
using NLog.Web;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private readonly ConcurrentBag<IStore> _createdStores = new();

    public OrchardApplicationFactory(
        ICounterDataCollector counterDataCollector,
        Action<IConfigurationBuilder> configureHost = null,
        Action<IWebHostBuilder> configuration = null,
        Action<ConfigurationManager, OrchardCoreBuilder> configureOrchard = null)
    {
        _counterDataCollector = counterDataCollector;
        _configureHost = configureHost;
        _configuration = configuration;
        _configureOrchard = configureOrchard;
    }

    public Uri BaseAddress => ClientOptions.BaseAddress;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(configurationBuilder => _configureHost?.Invoke(configurationBuilder));

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
        services.AddSingleton(_counterDataCollector);

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

        builder.Configure(app => app.UseMiddleware<PageLoadProbeMiddleware>())
            .ConfigureServices(builderServices =>
            {
                AddFakeStore(builderServices);
                AddFakeViewCompilerProvider(builderServices);
                AddSessionProbe(builderServices);
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

            return new SessionProbe(
                _counterDataCollector,
                httpContextAccessor.HttpContext.Request.Method,
                new Uri(httpContextAccessor.HttpContext.Request.GetEncodedUrl()),
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
