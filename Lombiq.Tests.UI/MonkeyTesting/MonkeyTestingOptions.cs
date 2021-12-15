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
        /// Gets or sets the base random seed. The default value is random. Can be used to reproduce a test with a same
        /// randomization.
        /// </summary>
        public int BaseRandomSeed { get; set; } = RandomNumberGenerator.GetInt32(100_000);

        /// <summary>
        /// Gets or sets the page test time. The default value is 60 seconds.
        /// </summary>
        public TimeSpan PageTestTime { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Gets or sets the page marker polling interval. The default value is 200 milliseconds.
        /// </summary>
        public TimeSpan PageMarkerPollingInterval { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Gets or sets a value indicating whether to run accessibility checking assertion. The default value is <see
        /// langword="true"/>.
        /// </summary>
        public bool RunAccessibilityCheckingAssertion { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to run HTML validation assertion. The default value is <see
        /// langword="true"/>.
        /// </summary>
        public bool RunHtmlValidationAssertion { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to run application log assertion. The default value is <see
        /// langword="true"/>.
        /// </summary>
        public bool RunAppLogAssertion { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to run browser log assertion. The default value is <see
        /// langword="true"/>.
        /// </summary>
        public bool RunBrowserLogAssertion { get; set; } = true;

        /// <summary>
        /// <para>Gets the gremlins' species.</para>
        /// <para>By default contains:</para>
        /// <list type="number">
        /// <item><description><c>"gremlins.species.clicker()"</c></description></item>
        /// <item><description><c>"gremlins.species.toucher()"</c></description></item>
        /// <item><description><c>"gremlins.species.formFiller()"</c></description></item>
        /// <item><description><c>"gremlins.species.scroller()"</c></description></item>
        /// <item><description><c>"gremlins.species.typer()"</c></description></item>
        /// </list>
        /// </summary>
        public List<string> GremlinsSpecies { get; } = new()
        {
            "gremlins.species.clicker()",
            "gremlins.species.toucher()",
            "gremlins.species.formFiller()",
            "gremlins.species.scroller()",
            "gremlins.species.typer()",
        };

        /// <summary>
        /// <para>Gets the gremlins' mogwais.</para>
        /// <para>By default contains:</para>
        /// <list type="number">
        /// <item><description><c>"gremlins.mogwais.alert()"</c></description></item>
        /// <item><description><c>"gremlins.mogwais.gizmo()"</c></description></item>
        /// </list>
        /// </summary>
        public List<string> GremlinsMogwais { get; } = new()
        {
            "gremlins.mogwais.alert()",
            "gremlins.mogwais.gizmo()",
        };

        /// <summary>
        /// Gets or sets the gremlins attack delay. The default value is 10 milliseconds.
        /// </summary>
        public TimeSpan GremlinsAttackDelay { get; set; } = TimeSpan.FromMilliseconds(10);

        /// <summary>
        /// <para>Gets the URL sanitizers.</para>
        /// <para>By default contains:</para>
        /// <list type="number">
        /// <item><description><see cref="RemovesFragmentMonkeyTestingUrlSanitizer"/> instance.</description></item>
        /// <item><description><see cref="RemovesBaseUrlMonkeyTestingUrlSanitizer"/> instance.</description></item>
        /// <item><description><see cref="RemovesQueryParameterMonkeyTestingUrlSanitizer"/> instance with <c>"admin"</c>
        /// argument.</description></item>
        /// <item><description><see cref="RemovesQueryParameterMonkeyTestingUrlSanitizer"/> instance with
        /// <c>"returnUrl"</c> argument.</description>
        /// </item>
        /// </list>
        /// </summary>
        public List<IMonkeyTestingUrlSanitizer> UrlSanitizers { get; } = new()
        {
            new RemovesFragmentMonkeyTestingUrlSanitizer(),
            new RemovesBaseUrlMonkeyTestingUrlSanitizer(),
            new RemovesQueryParameterMonkeyTestingUrlSanitizer("admin"),
            new RemovesQueryParameterMonkeyTestingUrlSanitizer("returnUrl"),
        };

        /// <summary>
        /// <para>Gets the URL filters.</para>
        /// <para>By default contains:</para>
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
