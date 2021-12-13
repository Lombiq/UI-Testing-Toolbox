namespace Lombiq.Tests.UI.MonkeyTesting
{
    internal sealed class GremlinsScriptBuilder
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
    function callback() {{
        gremlins.createHorde({{
            species: [{speciesPart}],
            mogwais: [{mogwaisPart}],
            strategies: [
                gremlins.strategies.distribution({{
                    nb: {NumberOfAttacks},
                    delay: {AttackDelay}
                }})
            ],
            randomizer: new gremlins.Chance({RandomSeed})
        }}).unleash();
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
