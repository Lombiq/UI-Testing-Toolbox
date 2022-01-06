using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    internal static class GremlinsScripts
    {
        internal const string GetAreGremlinsRunningScript = "return !!window.activeGremlinsHorde;";

        internal const string StopGremlinsScript =
@"var horde = window.activeGremlinsHorde;
if (horde)
    horde.stop();";

        private static readonly Lazy<string> _lazyGremlinsScript = new(
            () => EmbeddedResourceProvider.ReadEmbeddedFile("gremlins.min.js"));

        internal static string GremlinsScript => _lazyGremlinsScript.Value;

        internal sealed class RunScriptBuilder
        {
            internal string[] Species { get; set; }

            internal string[] Mogwais { get; set; }

            internal int NumberOfAttacks { get; set; }

            internal int AttackDelay { get; set; }

            internal int RandomSeed { get; set; }

            internal string Build()
            {
                string speciesPart = Species != null ? string.Join(", ", Species) : null;

                string mogwaisPart = Mogwais != null ? string.Join(", ", Mogwais) : null;

                return
@$"(function() {{
    window.activeGremlinsHorde = gremlins.createHorde({{
        species: [{speciesPart}],
        mogwais: [{mogwaisPart}],
        strategies: [
            gremlins.strategies.distribution({{
                nb: {NumberOfAttacks.ToTechnicalString()},
                delay: {AttackDelay.ToTechnicalString()}
            }})
        ],
        randomizer: new gremlins.Chance({RandomSeed.ToTechnicalString()})
    }});

    window.activeGremlinsHorde.unleash()
        .then(() => {{
            window.activeGremlinsHorde = null;
        }});
}})();";
            }
        }
    }
}
