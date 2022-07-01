using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;

namespace Lombiq.Tests.UI.Extensions;
public static class ElementStyleUITestContextExtensions
{
    public static void SetElementStyle(this UITestContext context, By elementSelector, string rule, string value) =>
        context.ExecuteScript(
            "arguments[0].style[arguments[1]] = arguments[2];",
            context.Get(elementSelector),
            rule,
            value);
}
