using Lombiq.Tests.UI.Services;
using System;
using System.Text.RegularExpressions;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    /// <summary>
    /// URL sanitizer that removes parts that match the specific regex pattern.
    /// </summary>
    public class RemovesByRegexMonkeyTestingUrlSanitizer : IMonkeyTestingUrlSanitizer
    {
        private readonly Regex _regex;

        public RemovesByRegexMonkeyTestingUrlSanitizer(string regexPattern)
            : this(new Regex(regexPattern, RegexOptions.Compiled))
        {
        }

        public RemovesByRegexMonkeyTestingUrlSanitizer(Regex regex) =>
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
