using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Models;

public class OrchardCoreAppStartContext
{
    public string ContentRootPath { get; set; }
    public Uri Url { get; set; }
    public PortLeaseManager PortLeaseManager { get; set; }
}
