namespace Lombiq.Tests.UI.Shortcuts;

public static class ShortcutsFeatureIds
{
    internal const string DescriptionUiTestWarning = "WARNING: Only enable this feature in the UI testing environment. ";

    public const string Area = "Lombiq.Tests.UI.Shortcuts";

    public const string Default = Area;
    public const string FeatureToggleTestBench = $"{Default}.{nameof(FeatureToggleTestBench)}";
    public const string MediaCachePurge = $"{Default}.{nameof(MediaCachePurge)}";
    public const string ShiftTime = $"{Default}.{nameof(ShiftTime)}";
    public const string Swagger = $"{Default}.{nameof(Swagger)}";
    public const string Workflows = $"{Default}.{nameof(Workflows)}";
}
