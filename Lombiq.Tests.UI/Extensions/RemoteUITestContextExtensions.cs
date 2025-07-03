using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Extensions;

public static class RemoteUITestContextExtensions
{
    public static bool IsLocaleUITest(this UITestContext context) => context.TestStartUri.Host.ContainsOrdinalIgnoreCase("localhost");
}
