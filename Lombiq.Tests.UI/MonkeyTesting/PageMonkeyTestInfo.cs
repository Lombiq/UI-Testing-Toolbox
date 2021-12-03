using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    internal sealed class PageMonkeyTestInfo
    {
        internal PageMonkeyTestInfo(string url, string cleanUrl, TimeSpan timeToTest)
        {
            Url = url;
            CleanUrl = cleanUrl;
            TimeToTest = timeToTest;
        }

        internal string Url { get; }

        internal string CleanUrl { get; }

        internal TimeSpan TimeToTest { get; set; }

        internal bool HasTimeToTest => TimeToTest > TimeSpan.Zero;
    }
}
