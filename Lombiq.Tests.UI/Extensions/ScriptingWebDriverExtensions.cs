using Atata;

namespace OpenQA.Selenium;

public static class ScriptingWebDriverExtensions
{
    public static object ExecuteScript(this IWebDriver driver, string script, params object[] arguments) =>
        driver.AsScriptExecutor().ExecuteScript(script, arguments);

    public static object ExecuteAsyncScript(this IWebDriver driver, string script, params object[] arguments) =>
        driver.AsScriptExecutor().ExecuteAsyncScript(script, arguments);
}
