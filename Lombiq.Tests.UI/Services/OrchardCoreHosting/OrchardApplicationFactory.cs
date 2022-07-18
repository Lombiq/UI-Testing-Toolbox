using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services.OrchardCoreHosting;

// Taken from: https://github.com/benday-inc/SeleniumDemo.
public class OrchardApplicationFactory<TStartup> : WebApplicationFactory<TStartup>
   where TStartup : class
{
    private readonly Action<IWebHostBuilder> _configuration;
    private IHost _host;

    public OrchardApplicationFactory(string rootUrl, Action<IWebHostBuilder> configuration = null)
    {
        RootUrl = rootUrl;
        _configuration = configuration;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => _configuration?.Invoke(builder);

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = builder.Build();

        // configure and start the actual host.
        builder.ConfigureWebHost(webHostBuilder =>
            webHostBuilder.UseKestrel()
                .UseUrls(RootUrl));

        _host = builder.Build();
        _host.Start();

        return host;
    }

    // WebApplicationFactory<>.DisposeAsync() calls "GC.SuppressFinalize".
#pragma warning disable CA1816 // Call GC.SuppressFinalize correctly
    public override async ValueTask DisposeAsync()
#pragma warning restore CA1816 // Call GC.SuppressFinalize correctly
    {
        await _host?.StopAsync();
        _host?.Dispose();
        _host = null;

        await base.DisposeAsync();
    }

    public string RootUrl { get; private set; }
}
