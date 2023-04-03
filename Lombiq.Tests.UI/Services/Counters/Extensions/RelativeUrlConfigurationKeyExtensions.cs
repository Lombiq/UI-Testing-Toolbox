namespace Lombiq.Tests.UI.Services.Counters.Extensions;

public static class RelativeUrlConfigurationKeyExtensions
{
    public static bool EqualsWith(this IRelativeUrlConfigurationKey left, IRelativeUrlConfigurationKey right)
    {
        if (ReferenceEquals(left, right)) return true;

        if (left is null || right is null) return false;

        return left.Url?.PathAndQuery == right.Url?.PathAndQuery;
    }
}
