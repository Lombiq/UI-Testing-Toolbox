using Microsoft.AspNetCore.Mvc;

namespace Lombiq.Tests.UI.Shortcuts.Controllers
{
    public class CurrentUserController : Controller
    {
        public string Index() => User?.Identity?.Name;
    }
}
