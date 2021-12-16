using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    internal sealed class PageMonkeyTestInfo
    {
        internal PageMonkeyTestInfo(string url, string sanitizedUrl, TimeSpan timeToTest)
        {
            Url = url;
            SanitizedUrl = sanitizedUrl;
            TimeToTest = timeToTest;
        }

        internal string Url { get; }

        internal string SanitizedUrl { get; }

        internal TimeSpan TimeToTest { get; set; }

        internal bool HasTimeToTest => TimeToTest > TimeSpan.Zero;
    }
}
