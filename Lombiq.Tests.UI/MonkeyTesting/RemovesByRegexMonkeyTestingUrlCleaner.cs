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

        public Uri Clean(UITestContext context, Uri url)
        {
            string urlAsString = url.OriginalString;

            if (_regex.IsMatch(urlAsString))
            {
                string processedUrl = _regex.Replace(urlAsString, string.Empty);
                return new(processedUrl, UriKind.RelativeOrAbsolute);
            }

            return url;
        }
    }
}
