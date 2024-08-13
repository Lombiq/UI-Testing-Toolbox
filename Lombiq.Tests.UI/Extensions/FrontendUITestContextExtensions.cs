using Jint;
using Lombiq.Tests.UI.Services;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

public static class FrontendUITestContextExtensions
{
    /// <summary>
    /// Sets up a Javascript environment with access to the <paramref name="context"/> and executes it.
    /// </summary>
    /// <param name="script">The Javascript source code to execute.</param>
    public static async Task ExecuteJavascriptTestAsync(this UITestContext context, string script)
    {
        using var engine = new Engine(configuration => configuration.AllowClr());
        engine.SetValue(nameof(context), context);

        engine.Execute(script);
    }
}
