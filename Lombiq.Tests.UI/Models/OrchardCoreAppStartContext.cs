using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Models;

public record OrchardCoreAppStartContext(string ContentRootPath, Uri Url, PortLeaseManager PortLeaseManager);
