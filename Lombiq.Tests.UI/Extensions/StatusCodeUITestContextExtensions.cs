using Lombiq.Tests.UI.Services;
using Shouldly;

namespace Lombiq.Tests.UI.Extensions
{
    public static class StatusCodeUITestContextExtensions
    {
        public static void AssertStatusCodeOnUrl(this UITestContext context, string url, int statusCode) =>
            context.ExecuteAsyncScript(
                @"var url = arguments[0];
                var callback = arguments[arguments.length - 1];
                fetch(url).then(function(response) {
                    callback(response.status);
                });",
                url).ShouldBe(statusCode);

        public static void AssertNotFoundResultOnUrl(this UITestContext context, string url) =>
            context.AssertStatusCodeOnUrl(url, 404);

        public static void AssertSuccessResultOnUrl(this UITestContext context, string url) =>
            context.AssertStatusCodeOnUrl(url, 200);
    }
}
