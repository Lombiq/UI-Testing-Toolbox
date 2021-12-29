using Lombiq.Tests.UI.Services;
using System;
using System.Text.RegularExpressions;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    /// <summary>
    /// URL filter that matches the URL against the configured regex pattern.
    /// </summary>
    public class MatchesRegexMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        private readonly Regex _regex;

        public MatchesRegexMonkeyTestingUrlFilter(string regexPattern)
            : this(new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.ExplicitCapture))
        {
        }

        public MatchesRegexMonkeyTestingUrlFilter(Regex regex) =>
            _regex = regex ?? throw new ArgumentNullException(nameof(regex));

        public bool AllowUrl(UITestContext context, Uri url) => _regex.IsMatch(url.AbsoluteUri);
    }
}
