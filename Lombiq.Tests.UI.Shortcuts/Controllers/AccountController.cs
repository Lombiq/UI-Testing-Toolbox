using Lombiq.HelpfulLibraries.Libraries.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Users;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers
{
    [DevelopmentAndLocalhostOnly]
    public class AccountController : Controller
    {
        private readonly UserManager<IUser> _userManager;
        private readonly SignInManager<IUser> _userSignInManager;


        public AccountController(UserManager<IUser> userManager, SignInManager<IUser> userSignInManager)
        {
            _userManager = userManager;
            _userSignInManager = userSignInManager;
        }


        [AllowAnonymous]
        public async Task<IActionResult> SignInDirectly(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);

            if (user == null) return StatusCode(404);

            await _userSignInManager.SignInAsync(user, false);

            return StatusCode(200);
        }

        public async Task<IActionResult> SignOutDirectly()
        {
            await _userSignInManager.SignOutAsync();

            return StatusCode(200);
        }
    }
}
