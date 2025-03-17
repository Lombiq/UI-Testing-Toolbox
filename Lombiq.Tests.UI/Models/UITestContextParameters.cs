using Lombiq.Tests.UI.SecurityScanning;
using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Models;

public sealed record UITestContextParameters
{
    public string Id { get; init; }
    public UITestManifest TestManifest { get; init; }
    public OrchardCoreUITestExecutorConfiguration Configuration { get; init; }
    public IWebApplicationInstance Application { get; init; }
    public AtataScope Scope { get; init; }
    public RunningContextContainer RunningContextContainer { get; init; }
    public ZapManager ZapManager { get; init; }
    public CounterDataCollector CounterDataCollector { get; init; }
    public Uri TestStartUri { get; init; }
}
