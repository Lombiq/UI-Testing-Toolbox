using System.Collections.Generic;
using System.Globalization;

namespace Lombiq.Tests.UI.Models;

public class InstanceCommandLineArgumentsBuilder
{
    private readonly List<string> _arguments = [];

    public IEnumerable<string> Arguments => _arguments;

    public InstanceCommandLineArgumentsBuilder AddSwitch(string argument)
    {
        _arguments.Add($"{PrepareArg(argument)}");

        return this;
    }

    public InstanceCommandLineArgumentsBuilder AddWithValue<T>(string key, T value)
    {
        // MA0185 doesn't apply: value can be a culture-sensitive type (e.g. number, date) at runtime.
#pragma warning disable MA0185
        _arguments.Add(string.Create(CultureInfo.InvariantCulture, $"{PrepareArg(key)}={value}"));
#pragma warning restore MA0185

        return this;
    }

    private static string PrepareArg(string argument) => $"--{argument.TrimStart('-')}";
}
