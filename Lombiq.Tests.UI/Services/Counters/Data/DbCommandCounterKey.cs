using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Lombiq.Tests.UI.Services.Counters.Data;

public abstract class DbCommandCounterKey : CounterKey
{
    private readonly List<KeyValuePair<string, object>> _parameters = [];
    public string CommandText { get; private set; }
    public IEnumerable<KeyValuePair<string, object>> Parameters => _parameters;

    protected DbCommandCounterKey(string commandText, IEnumerable<KeyValuePair<string, object>> parameters)
    {
        _parameters.AddRange(parameters);
        CommandText = commandText;
    }

    public override bool Equals(ICounterKey other)
    {
        if (ReferenceEquals(this, other)) return true;

        return other is DbCommandCounterKey otherKey
            && other.GetType() == GetType()
            && GetType() == otherKey.GetType()
            && string.Equals(CommandText, otherKey.CommandText, StringComparison.OrdinalIgnoreCase)
            && Parameters.Any()
            && Parameters
                .Select(param => (param.Key, param.Value))
                .SequenceEqual(otherKey.Parameters.Select(param => (param.Key, param.Value)));
    }

    public override IEnumerable<string> Dump()
    {
        var lines = new List<string>
        {
            DisplayName,
            $"\tQuery: {CommandText}",
        };

        if (Parameters.Any())
        {
            var commandParams = Parameters.Select((parameter, index) =>
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"[{index}]{parameter.Key ?? string.Empty} = {parameter.Value?.ToString() ?? "(null)"}"))
                .Join(", ");
            lines.Add($"\t\tParameters: {commandParams}");
        }

        return lines;
    }

    protected override int HashCode() => StringComparer.Ordinal.GetHashCode(CommandText.ToUpperInvariant());
    public override string ToString() => $"[{GetType().Name}] {CommandText}";
}
