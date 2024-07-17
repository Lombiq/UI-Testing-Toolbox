using System;

namespace Lombiq.Tests.UI.Services.Counters.Extensions;

public static class RelativeUrlConfigurationKeyExtensions
{
    public static bool EqualsWith(this IRelativeUrlConfigurationKey left, IRelativeUrlConfigurationKey right)
    {
        if (ReferenceEquals(left, right)) return true;

        if (left?.Url is null || right?.Url is null) return false;

        var leftUrl = left.Url.IsAbsoluteUri ? left.Url.PathAndQuery : left.Url.OriginalString;
        var rightUrl = right.Url.IsAbsoluteUri ? right.Url.PathAndQuery : right.Url.OriginalString;

        return (left.ExactMatch || right.ExactMatch)
            ? string.Equals(leftUrl, rightUrl, StringComparison.OrdinalIgnoreCase)
            : leftUrl.EqualsOrdinalIgnoreCase(rightUrl) || rightUrl.EqualsOrdinalIgnoreCase(leftUrl);
    }
}
