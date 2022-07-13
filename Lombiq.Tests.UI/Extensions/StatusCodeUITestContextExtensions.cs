using Lombiq.Tests.UI.Services;
using Shouldly;

namespace Lombiq.Tests.UI.Extensions;

public static class StatusCodeUITestContextExtensions
{
    /// <summary>
    /// Opens the given URL asynchronously and checks the HTTP response code.
    /// </summary>
    /// <param name="url">Relative URL to open.</param>
    /// <param name="statusCode">Status code to assert.</param>
    public static void AssertStatusCodeOnUrl(this UITestContext context, string url, int statusCode) =>
        context.ExecuteAsyncScript(
            @"var url = arguments[0];
                var callback = arguments[arguments.length - 1];
                fetch(url).then(function(response) {
                    callback(response.status);
                });",
            url)
        .ShouldBe(statusCode);

    /// <summary>
    /// Opens the given URL asynchronously and checks if the HTTP response code is 404.
    /// </summary>
    /// <param name="url">Relative URL to open.</param>
    public static void AssertNotFoundResultOnUrl(this UITestContext context, string url) =>
        context.AssertStatusCodeOnUrl(url, 404);

    /// <summary>
    /// Opens the given URL asynchronously and checks if the HTTP response code is 200.
    /// </summary>
    /// <param name="url">Relative URL to open.</param>
    public static void AssertSuccessResultOnUrl(this UITestContext context, string url) =>
        context.AssertStatusCodeOnUrl(url, 200);
}
