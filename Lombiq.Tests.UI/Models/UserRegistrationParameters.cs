using Lombiq.Tests.UI.Constants;
using System;

namespace Lombiq.Tests.UI.Models;

public class UserRegistrationParameters
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }

    [Obsolete("Use CreateTest() instead.")]
    public static UserRegistrationParameters CreateDefault() =>
        new()
        {
            UserName = "TestUser",
            Email = "testuser@example.org", // #spell-check-ignore-line
            Password = DefaultUser.Password,
            ConfirmPassword = DefaultUser.Password,
        };

    public static UserRegistrationParameters CreateTest() =>
        new()
        {
            UserName = TestUser.UserName,
            Email = TestUser.Email,
            Password = TestUser.Password,
            ConfirmPassword = TestUser.Password,
        };
}
