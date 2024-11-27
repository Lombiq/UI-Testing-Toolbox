using Lombiq.Tests.UI.Services;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class WebApplicationInstanceUITestContextExtensions
{
    /// <summary>
    /// Restarts the application and refreshes the current page to warm it up.
    /// </summary>
    public static async Task RestartAndWarmUpApplicationAsync(this UITestContext context)
    {
        await context.Application.RestartAsync();
        context.Refresh();
    }
}
