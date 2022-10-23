using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lombiq.Tests.UI.Services.Counters.Data;

public abstract class DbCommandCounterKey : CounterKey
{
    private readonly List<KeyValuePair<string, object>> _parameters = new();
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

        return other is DbExecuteCounterKey otherKey
            && GetType() == otherKey.GetType()
            && CommandText == otherKey.CommandText
            && Parameters
                .Select(param => (param.Key, param.Value))
                .SequenceEqual(otherKey.Parameters.Select(param => (param.Key, param.Value)));
    }

    public override string Dump()
    {
        var builder = new StringBuilder();

        builder.AppendLine(GetType().Name)
            .AppendLine(CultureInfo.InvariantCulture, $"\t{CommandText}");
        var commandParams = Parameters.Select((parameter, index) =>
            FormattableString.Invariant(
                $"[{index.ToTechnicalString()}]{parameter.Key ?? string.Empty} = {parameter.Value?.ToString() ?? "(null)"}"))
            .Join(", ");
        builder.AppendLine(CultureInfo.InvariantCulture, $"\t\t{commandParams}");

        return builder.ToString();
    }

    protected override int HashCode() => StringComparer.Ordinal.GetHashCode(CommandText);
    public override string ToString() => $"[{GetType().Name}] {CommandText}";
}
