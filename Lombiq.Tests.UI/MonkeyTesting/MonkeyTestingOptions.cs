using Lombiq.Tests.UI.MonkeyTesting.UrlFilters;
using Lombiq.Tests.UI.MonkeyTesting.UrlSanitizers;
using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    /// <summary>
    /// Represents the options of monkey testing.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Design",
        "MA0016:Prefer return collection abstraction instead of implementation",
        Justification = "Implementations are adequate since this is a configuration class.")]
    public sealed class MonkeyTestingOptions
    {
        /// <summary>
        /// Gets or sets the base random seed. The default value is 1234 for reproducibility. Can be used to reproduce
        /// a test with a same randomization.
        /// </summary>
        public int BaseRandomSeed { get; set; } = 1234;

        /// <summary>
        /// Gets or sets the time spent monkey testing a given page. The default value is 60 seconds. The bigger this
        /// timespan the larger the chance that a given interaction will happen and thus a bug will be uncovered.
        /// </summary>
        public TimeSpan PageTestTime { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Gets or sets the page marker polling interval. The default value is 200 milliseconds. It is an interval of
        /// checking whether an inserted unique page marker is still on the page. The page marker not being found means
        /// the page is changed due to some click or other monkey activity.
        /// </summary>
        public TimeSpan PageMarkerPollingInterval { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// <para>Gets the gremlins' species.</para>
        /// <para>By default contains:</para>
        /// <list type="number">
        /// <item><description><c>"gremlins.species.clicker({ log: true })"</c></description></item>
        /// <item><description><c>"gremlins.species.toucher()"</c></description></item>
        /// <item><description><c>"gremlins.species.formFiller()"</c></description></item>
        /// <item><description><c>"gremlins.species.scroller()"</c></description></item>
        /// <item><description><c>"gremlins.species.typer()"</c></description></item>
        /// </list>
        /// </summary>
        public List<string> GremlinsSpecies { get; } = new()
        {
            "gremlins.species.clicker({ log: true })",
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
        /// Gets or sets the gremlins' attack delay. The default value is 10 milliseconds. Set greater time value to
        /// make gremlin interaction intervals smaller.
        /// </summary>
        public TimeSpan GremlinsAttackDelay { get; set; } = TimeSpan.FromMilliseconds(10);

        /// <summary>
        /// <para>Gets the URL sanitizers that can clean the URLs under test.</para>
        /// <para>By default contains:</para>
        /// <list type="number">
        /// <item><description><see cref="RemovesFragmentMonkeyTestingUrlSanitizer"/> instance.</description></item>
        /// <item><description><see cref="RemovesBaseUrlMonkeyTestingUrlSanitizer"/> instance.</description></item>
        /// <item><description><see cref="RemovesQueryParameterMonkeyTestingUrlSanitizer"/> instance with <c>"admin"</c>
        /// argument, because this argument varies on admin pages.</description></item>
        /// <item><description><see cref="RemovesQueryParameterMonkeyTestingUrlSanitizer"/> instance with
        /// <c>"returnUrl"</c> argument, because this argument varies on the login page.</description>
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
        /// <para>
        /// Gets the URL filters that can allow/disallow certain URLs. Note that tests can still navigate away from the
        /// allowed URLs but after opening a disallowed one they'll immediately return to the ones allowed.
        /// </para>
        /// <para>By default contains:</para>
        /// <list type="number">
        /// <item><description><see cref="StartsWithBaseUrlMonkeyTestingUrlFilter"/> instance.</description></item>
        /// </list>
        /// </summary>
        public List<IMonkeyTestingUrlFilter> UrlFilters { get; } = new()
        {
            new StartsWithBaseUrlMonkeyTestingUrlFilter(),
        };

        /// <summary>
        /// Adds the login page (/Login) to the URL filters (i.e. monkey testing will not happen on that page).
        /// </summary>
        public MonkeyTestingOptions ExcludeLoginPageFromMonkeyTesting()
        {
            UrlFilters.Add(new NotStartsWithMonkeyTestingUrlFilter("/Login"));
            return this;
        }

        /// <summary>
        /// Adds the register page (/Register) to the URL filters (i.e. monkey testing will not happen on that page).
        /// </summary>
        public MonkeyTestingOptions ExcludeRegisterPageFromMonkeyTesting()
        {
            UrlFilters.Add(new NotStartsWithMonkeyTestingUrlFilter("/Register"));
            return this;
        }

        /// <summary>
        /// Adds the login page (/Login) and the register page (/Register) to the URL filters (i.e. monkey testing will
        /// not happen on those pages).
        /// </summary>
        public MonkeyTestingOptions ExcludeLoginAndRegisterPagesFromMonkeyTesting() =>
            ExcludeLoginPageFromMonkeyTesting().ExcludeRegisterPageFromMonkeyTesting();
    }
}
