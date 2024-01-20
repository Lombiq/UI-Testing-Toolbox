using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Lombiq.Tests.UI.Services.OrchardCoreHosting;

public class FakeViewCompilerProvider(IServiceProvider services) : IViewCompilerProvider
{
    public IViewCompiler GetCompiler() =>
        services
            .GetServices<IViewCompilerProvider>()
            .FirstOrDefault()
            .GetCompiler();
}
