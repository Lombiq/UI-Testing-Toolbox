using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Extensions;

public static class BrowserLogMessageEnumerableExtensions
{
    public static string ToFormattedString(this IEnumerable<BrowserLogMessage> messages) =>
        string.Join(Environment.NewLine, messages);
}
