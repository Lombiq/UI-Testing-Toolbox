using System.Collections.Generic;
using System.Globalization;

namespace Lombiq.Tests.UI.Models;

public class InstanceCommandLineArgumentsBuilder
{
    private readonly List<string> _arguments = new();

    public IEnumerable<string> Arguments => _arguments;

    public InstanceCommandLineArgumentsBuilder AddSwitch(string argument)
    {
        _arguments.Add($"{PrepareArg(argument)}");

        return this;
    }

    public InstanceCommandLineArgumentsBuilder AddWithValue<T>(string key, T value)
    {
        _arguments.Add(string.Create(CultureInfo.InvariantCulture, $"{PrepareArg(key)}={value}"));

        return this;
    }

    private static string PrepareArg(string argument) => $"--{argument.TrimStart('-')}";
}
