using Lombiq.Tests.UI.SecurityScanning;
using Lombiq.Tests.UI.Services;
using System;
using Xunit;

namespace Lombiq.Tests.UI.Models;

public record UITestContextData(
    string Id,
    UITestManifest TestManifest,
    OrchardCoreUITestExecutorConfiguration Configuration,
    IWebApplicationInstance Application,
    ITestOutputHelper TestOutputHelper,
    Uri AppBaseUri,
    Uri TestStartUri,
    RunningContextContainer RunningContextContainer,
    ZapManager ZapManager);
