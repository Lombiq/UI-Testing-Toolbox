using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.Security;
using OrchardCore.Security.Permissions;
using OrchardCore.Users;
using OrchardCore.Users.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Controllers;

[DevelopmentAndLocalhostOnly]
[AllowAnonymous]
public class SecurityController : Controller
{
    private readonly RoleManager<IRole> _roleManager;
    private readonly UserManager<IUser> _userManager;
    private readonly IEnumerable<IPermissionProvider> _permissionProviders;

    public SecurityController(
        RoleManager<IRole> roleManager,
        UserManager<IUser> userManager,
        IEnumerable<IPermissionProvider> permissionProviders)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _permissionProviders = permissionProviders;
    }

    public async Task<IActionResult> AddUserToRole(string userName, string roleName)
    {
        if ((await _userManager.FindByNameAsync(userName)) is not User user)
        {
            return NotFound();
        }

        if ((await _roleManager.FindByNameAsync(_roleManager.NormalizeKey(roleName))) is not Role role)
        {
            return NotFound();
        }

        await _userManager.AddToRoleAsync(user, role.NormalizedRoleName);

        return Ok("Success");
    }

    public async Task<IActionResult> AddPermissionToRole(string permissionName, string roleName)
    {
        if ((await _roleManager.FindByNameAsync(_roleManager.NormalizeKey(roleName))) is not Role role)
        {
            return NotFound();
        }

        var permissionClaim = role.RoleClaims.FirstOrDefault(roleClaim =>
            roleClaim.ClaimType == Permission.ClaimType
            && roleClaim.ClaimValue == permissionName);
        if (permissionClaim == null)
        {
            if (!await PermissionExistsAsync(permissionName))
            {
                return NotFound();
            }

            role.RoleClaims.Add(new() { ClaimType = Permission.ClaimType, ClaimValue = permissionName });

            await _roleManager.UpdateAsync(role);
        }

        return Ok("Success");
    }

    private async Task<bool> PermissionExistsAsync(string permissionName) =>
        (await Task.WhenAll(_permissionProviders.Select(provider => provider.GetPermissionsAsync())))
            .SelectMany(permissions => permissions)
            .Any(permission => permission.Name == permissionName);
}
