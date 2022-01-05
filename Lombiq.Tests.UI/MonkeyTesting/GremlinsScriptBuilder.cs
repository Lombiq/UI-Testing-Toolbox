using System;

namespace Lombiq.Tests.UI.MonkeyTesting
{
    internal sealed class GremlinsScriptBuilder
    {
        public const string GetAreGremlinsRunningScript = "return !!window.areGremlinsRunning;";

        public const string StopGremlinsScript =
@"var horde = window.activeGremlinsHorde;
if (horde)
    horde.stop();";

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
    window.areGremlinsRunning = true;

    function callback() {{
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
                window.areGremlinsRunning = false;
                window.activeGremlinsHorde = null;
            }});
    }}
    var s = document.createElement('script');
    s.src = 'https://unpkg.com/gremlins.js';
    if (s.addEventListener) {{
        s.addEventListener('load', callback, false);
    }} else if (s.readyState) {{
        s.onreadystatechange = callback;
    }}
    document.body.appendChild(s);
}})();";
        }
    }
}
