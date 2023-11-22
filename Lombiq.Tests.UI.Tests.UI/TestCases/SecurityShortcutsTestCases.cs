using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using Lombiq.Tests.UI.Services;
using Shouldly;

namespace Lombiq.Tests.UI.Tests.UI.TestCases;

public static class SecurityShortcutsTestCases
{
    private const string UserUserName = "user";
    private const string UserEmail = "user@example.com";
    private const string AuthorRole = "Author";
    private const string FakeRole = "Fake";
    private const string ViewContentTypesPermission = "ViewContentTypes";
    private const string FakePermission = "Fake";

    public static Task AddUserToRoleShouldWorkAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync, Browser browser = default) =>
        executeTestAfterSetupAsync(
            async context =>
            {
                await CreateUserAndSignInAsync(context);

                await context.GoToContentTypesListAsync();
                context.GetCurrentUri().AbsolutePath.ShouldBe("/Error/403");
                await context.SignOutDirectlyAsync();

                await context.AddUserToRoleAsync(UserUserName, AuthorRole);
                await context.AddPermissionToRoleAsync(ViewContentTypesPermission, AuthorRole);

                await context.SignInDirectlyAsync(UserUserName);
                await context.GoToContentTypesListAsync();

                context.GetCurrentUri().AbsolutePath.ShouldBe("/Admin/ContentTypes/List");
            },
            browser,
            ConfigurationHelper.DisableHtmlValidation);

    public static Task AddUserToFakeRoleShouldThrowAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync, Browser browser = default) =>
        executeTestAfterSetupAsync(
            async context =>
            {
                await context.AddUserToRoleAsync(UserUserName, FakeRole).ShouldThrowAsync<UserNotFoundException>();

                await context.CreateUserAsync(UserUserName, DefaultUser.Password, UserEmail);
                await context.AddUserToRoleAsync(UserUserName, FakeRole).ShouldThrowAsync<RoleNotFoundException>();

                context.ClearLogs();
            },
            browser,
            ConfigurationHelper.DisableHtmlValidation);

    public static Task AllowFakePermissionToRoleShouldThrowAsync(
        ExecuteTestAfterSetupAsync executeTestAfterSetupAsync, Browser browser = default) =>
        executeTestAfterSetupAsync(
            async context =>
            {
                await context.AddPermissionToRoleAsync(FakePermission, AuthorRole)
                    .ShouldThrowAsync<PermissionNotFoundException>();

                context.ClearLogs();
            },
            browser,
            ConfigurationHelper.DisableHtmlValidation);

    private static async Task CreateUserAndSignInAsync(UITestContext context)
    {
        await context.CreateUserAsync(UserUserName, DefaultUser.Password, UserEmail);
        await context.SignInDirectlyAsync(UserUserName);
        (await context.GetCurrentUserNameAsync()).ShouldBe(UserUserName);
    }
}
