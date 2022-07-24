using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Lombiq.Tests.UI.Services.OrchardCoreHosting;

public class FakeViewCompilerProvider : IViewCompilerProvider
{
    private readonly IServiceProvider _services;

    public FakeViewCompilerProvider(IServiceProvider services) => _services = services;

    public IViewCompiler GetCompiler() =>
        _services
            .GetServices<IViewCompilerProvider>()
            .FirstOrDefault()
            .GetCompiler();
}
