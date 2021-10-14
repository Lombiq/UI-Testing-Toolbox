using Lombiq.Tests.UI.Constants;

namespace Lombiq.Tests.UI.Models
{
    public class UserRegistrationParameters
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

        public static UserRegistrationParameters CreateDefault() =>
            new()
            {
                UserName = "TestUser",
                Email = "testuser@example.org",
                Password = DefaultUser.Password,
                ConfirmPassword = DefaultUser.Password,
            };
    }
}
