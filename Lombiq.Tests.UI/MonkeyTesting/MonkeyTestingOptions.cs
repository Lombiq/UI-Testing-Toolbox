using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    /// <summary>
    /// Represents the options of monkey testing.
    /// </summary>
    public sealed class MonkeyTestingOptions
    {
        /// <summary>
        /// Gets or sets the base random seed.
        /// The default value is random.
        /// </summary>
        public int BaseRandomSeed { get; set; } = RandomNumberGenerator.GetInt32(100_000);

        /// <summary>
        /// Gets or sets the page test time.
        /// The default value is 60 seconds.
        /// </summary>
        public TimeSpan PageTestTime { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Gets or sets the page marker polling interval.
        /// The default value is 200 milliseconds.
        /// </summary>
        public TimeSpan PageMarkerPollingInterval { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Gets or sets a value indicating whether to run accessibility checking assertion.
        /// The default value is <see langword="true"/>.
        /// </summary>
        public bool RunAccessibilityCheckingAssertion { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to run HTML validation assertion.
        /// The default value is <see langword="true"/>.
        /// </summary>
        public bool RunHtmlValidationAssertion { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to run application log assertion.
        /// The default value is <see langword="true"/>.
        /// </summary>
        public bool RunAppLogAssertion { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to run browser log assertion.
        /// The default value is <see langword="true"/>.
        /// </summary>
        public bool RunBrowserLogAssertion { get; set; } = true;

        /// <summary>
        /// Gets the gremlins species.
        /// By default contains: <c>"clicker"</c>, <c>"toucher"</c>, <c>"formFiller"</c>, <c>"scroller"</c>, <c>"typer"</c>.
        /// </summary>
        public List<string> GremlinsSpecies { get; } = new()
        {
            "clicker",
            "toucher",
            "formFiller",
            "scroller",
            "typer",
        };

        /// <summary>
        /// Gets the gremlins mogwais.
        /// By default contains: <c>"alert"</c>, <c>"fps"</c>, <c>"gizmo"</c>.
        /// </summary>
        public List<string> GremlinsMogwais { get; } = new()
        {
            "alert",
            "fps",
            "gizmo",
        };

        /// <summary>
        /// Gets or sets the gremlins attack delay.
        /// The default value is 10 milliseconds.
        /// </summary>
        public TimeSpan GremlinsAttackDelay { get; set; } = TimeSpan.FromMilliseconds(10);

        /// <summary>
        /// <para>
        /// Gets the URL cleaners.
        /// </para>
        /// <para>
        /// By default contains:
        /// </para>
        /// <list type="number">
        /// <item><description><see cref="RemovesFragmentMonkeyTestingUrlCleaner"/> instance.</description></item>
        /// <item><description><see cref="RemovesBaseUrlMonkeyTestingUrlCleaner"/> instance.</description></item>
        /// <item><description><see cref="RemovesQueryParameterMonkeyTestingUrlCleaner"/> instance with <c>"admin"</c> argument.</description></item>
        /// <item><description><see cref="RemovesQueryParameterMonkeyTestingUrlCleaner"/> instance with <c>"returnUrl"</c> argument.</description>
        /// </item>
        /// </list>
        /// </summary>
        public List<IMonkeyTestingUrlCleaner> UrlCleaners { get; } = new()
        {
            new RemovesFragmentMonkeyTestingUrlCleaner(),
            new RemovesBaseUrlMonkeyTestingUrlCleaner(),
            new RemovesQueryParameterMonkeyTestingUrlCleaner("admin"),
            new RemovesQueryParameterMonkeyTestingUrlCleaner("returnUrl"),
        };

        /// <summary>
        /// <para>
        /// Gets the URL filters.
        /// </para>
        /// <para>
        /// By default contains:
        /// </para>
        /// <list type="number">
        /// <item><description><see cref="StartsWithBaseUrlMonkeyTestingUrlFilter"/> instance.</description></item>
        /// </list>
        /// </summary>
        public List<IMonkeyTestingUrlFilter> UrlFilters { get; } = new()
        {
            new StartsWithBaseUrlMonkeyTestingUrlFilter(),
        };
    }
}
