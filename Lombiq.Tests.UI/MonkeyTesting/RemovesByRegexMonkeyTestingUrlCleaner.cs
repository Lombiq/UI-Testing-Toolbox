using Lombiq.Tests.UI.Services;
using System;
using System.Text.RegularExpressions;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    public class RemovesByRegexMonkeyTestingUrlCleaner : IMonkeyTestingUrlCleaner
    {
        private readonly Regex _regex;

        public RemovesByRegexMonkeyTestingUrlCleaner(string regexPattern)
            : this(new Regex(regexPattern))
        {
        }

        public RemovesByRegexMonkeyTestingUrlCleaner(Regex regex) =>
            _regex = regex ?? throw new ArgumentNullException(nameof(regex));

        public string Handle(string url, UITestContext context) =>
            _regex.Replace(url, string.Empty);
    }
}
