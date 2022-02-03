using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    internal sealed class PageMonkeyTestInfo
    {
        internal PageMonkeyTestInfo(Uri url, Uri sanitizedUrl, TimeSpan timeToTest)
        {
            Url = url;
            SanitizedUrl = sanitizedUrl;
            TimeToTest = timeToTest;
        }

        internal Uri Url { get; }

        internal Uri SanitizedUrl { get; }

        internal TimeSpan TimeToTest { get; set; }

        internal bool HasTimeToTest => TimeToTest > TimeSpan.Zero;
    }
}
