using Lombiq.Tests.UI.Services;
using Newtonsoft.Json;
using OpenQA.Selenium;

namespace Lombiq.Tests.UI.Extensions;

public static class ScriptingUITestContextExtensions
{
    public static object ExecuteScript(this UITestContext context, string script, params object[] args) =>
        context.ExecuteLogged(nameof(ExecuteScript), script, () => context.Driver.ExecuteScript(script, args));

    public static object ExecuteAsyncScript(this UITestContext context, string script, params object[] args) =>
        context.ExecuteLogged(nameof(ExecuteAsyncScript), script, () => context.Driver.ExecuteAsyncScript(script, args));

    /// <summary>
    /// Uses JavaScript to set form inputs to values that are hard or impossible by normal means.
    /// </summary>
    public static void SetValueWithScript(this UITestContext context, string id, object value) =>
        ExecuteScript(
            context,
            $"document.getElementById({JsonConvert.SerializeObject(id)}).value = " +
            $"{JsonConvert.SerializeObject(value)};");
}
