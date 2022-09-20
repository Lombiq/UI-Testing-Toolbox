using Lombiq.Tests.UI.Constants;

namespace Lombiq.Tests.UI.Models;

public class CreateTenant
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseProvider { get; set; } = "Sqlite";
    public string Email { get; set; } = DefaultUser.Email;
    public string FeatureProfile { get; set; }
    public string Language { get; set; } = "en";
    public string Password { get; set; } = DefaultUser.Password;
    public string TimeZone { get; set; }
    public string UserName { get; set; } = DefaultUser.UserName;
}
