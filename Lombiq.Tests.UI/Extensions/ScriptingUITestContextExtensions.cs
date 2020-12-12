using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;

namespace Lombiq.Tests.UI.Extensions
{
    public static class ScriptingUITestContextExtensions
    {
        public static object ExecuteScript(this UITestContext context, string script, params object[] args) =>
            context.ExecuteLogged(nameof(ExecuteScript), script, () => ((IJavaScriptExecutor)context.Driver).ExecuteScript(script, args));

        public static object ExecuteAsyncScript(this UITestContext context, string script, params object[] args) =>
            context.ExecuteLogged(nameof(ExecuteScript), script, () => ((IJavaScriptExecutor)context.Driver).ExecuteAsyncScript(script, args));
    }
}
