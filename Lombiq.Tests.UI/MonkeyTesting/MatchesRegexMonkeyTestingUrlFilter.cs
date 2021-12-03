using Lombiq.Tests.UI.Services;
using System;
using System.Text.RegularExpressions;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    public class MatchesRegexMonkeyTestingUrlFilter : IMonkeyTestingUrlFilter
    {
        private readonly Regex _regex;

        public MatchesRegexMonkeyTestingUrlFilter(string regexPattern)
            : this(new Regex(regexPattern))
        {
        }

        public MatchesRegexMonkeyTestingUrlFilter(Regex regex) =>
            _regex = regex ?? throw new ArgumentNullException(nameof(regex));

        public bool CanHandle(string url, UITestContext context) =>
            _regex.IsMatch(url);
    }
}
