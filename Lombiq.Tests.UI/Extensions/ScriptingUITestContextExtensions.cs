using Atata;
using Lombiq.Tests.UI.Services;
using Newtonsoft.Json;

namespace Lombiq.Tests.UI.Extensions;

public static class ScriptingUITestContextExtensions
{
    public static object ExecuteScript(this UITestContext context, string script, params object[] args) =>
        context.ExecuteLogged(nameof(ExecuteScript), script, () => context.Driver.AsScriptExecutor().ExecuteScript(script, args));

    public static object ExecuteAsyncScript(this UITestContext context, string script, params object[] args) =>
        context.ExecuteLogged(nameof(ExecuteAsyncScript), script, () => context.Driver.AsScriptExecutor().ExecuteAsyncScript(script, args));

    /// <summary>
    /// Uses Javascript to set form inputs to values that are hard or impossible by normal means.
    /// </summary>
    public static void SetValueWithScript(this UITestContext context, string id, object value) =>
        ExecuteScript(
            context,
            $"document.getElementById({JsonConvert.SerializeObject(id)}).value = " +
            $"{JsonConvert.SerializeObject(value)};");
}
