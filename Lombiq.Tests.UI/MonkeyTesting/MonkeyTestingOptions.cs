using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    public sealed class MonkeyTestingOptions
    {
        public int BaseRandomSeed { get; set; } = RandomNumberGenerator.GetInt32(100_000);

        public TimeSpan PageTestTime { get; set; } = TimeSpan.FromSeconds(60);

        public TimeSpan PageMarkerPollingInterval { get; set; } = TimeSpan.FromMilliseconds(200);

        public bool RunAccessibilityCheckingAssertion { get; set; } = true;

        public bool RunHtmlValidationAssertion { get; set; } = true;

        public bool RunBrowserLogAssertion { get; set; } = true;

        public List<string> GremlinsSpecies { get; } = new()
        {
            "clicker",
            "toucher",
            "formFiller",
            "scroller",
            "typer",
        };

        public List<string> GremlinsMogwais { get; } = new()
        {
            "alert",
            "fps",
            "gizmo",
        };

        public TimeSpan GremlinsAttackDelay { get; set; } = TimeSpan.FromMilliseconds(10);

        public List<IMonkeyTestingUrlCleaner> UrlCleaners { get; } = new()
        {
            new RemovesFragmentMonkeyTestingUrlCleaner(),
            new RemovesBaseUrlMonkeyTestingUrlCleaner(),
            new RemovesQueryParameterMonkeyTestingUrlCleaner("admin"),
            new RemovesQueryParameterMonkeyTestingUrlCleaner("returnUrl"),
        };

        public List<IMonkeyTestingUrlFilter> UrlFilters { get; } = new()
        {
            new StartsWithBaseUrlMonkeyTestingUrlFilter(),
        };
    }
}
