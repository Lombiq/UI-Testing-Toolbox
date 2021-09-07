using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Lombiq.Tests.UI.Shortcuts.Controllers
{
    // This needs to be consumed directly from the browser.
    [DevelopmentAndLocalhostOnly]
    public class CurrentUserController : Controller
    {
        // Needs to return a string even if there's no user, otherwise it'd return an HTTP 204 without a body, see:
        // https://weblog.west-wind.com/posts/2020/Feb/24/Null-API-Responses-and-HTTP-204-Results-in-ASPNET-Core.
        public string Index() => "UserName: " + User?.Identity?.Name;
    }
}
