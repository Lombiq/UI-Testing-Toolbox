namespace Lombiq.Tests.UI.Constants;

public static class ConfigurationKeys
{
    public const string BaseKey = "Lombiq_Tests_UI";
    private const string Prefix = BaseKey + ":";

    public const string IsUITesting = Prefix + nameof(IsUITesting);
    public const string EnableSqlQueryMonitoring = Prefix + nameof(EnableSqlQueryMonitoring);
    public const string EnableSmtpFeature = Prefix + nameof(EnableSmtpFeature);
    public const string UseAzureBlobStorage = Prefix + nameof(UseAzureBlobStorage);
    public const string InjectApplicationInfo = Prefix + nameof(InjectApplicationInfo);
}
