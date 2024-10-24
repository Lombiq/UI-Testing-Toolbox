using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System.Text.Json;

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
            $"document.getElementById({JsonSerializer.Serialize(id)}).value = {JsonSerializer.Serialize(value)};");

    /// <summary>
    /// Uses JavaScript to set textarea values that are hard or impossible by normal means.
    /// </summary>
    public static void SetTextContentWithScript(this UITestContext context, string textareaId, object value) =>
        ExecuteScript(
            context,
            $"document.getElementById({JsonSerializer.Serialize(textareaId)}).textContent = {JsonSerializer.Serialize(value)};");
}
