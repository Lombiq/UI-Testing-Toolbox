using System.Collections.Generic;

namespace Lombiq.Tests.UI.Models;

public class InstanceCommandLineArgumentsBuilder
{
    private readonly List<string> _arguments = new();

    public InstanceCommandLineArgumentsBuilder AddSwitch(string argument)
    {
        _arguments.Add($"{PrepareArg(argument)}");

        return this;
    }

    public InstanceCommandLineArgumentsBuilder AddWithValue<T>(string key, T value)
    {
        _arguments.Add($"{PrepareArg(key)}={value}");

        return this;
    }

    private static string PrepareArg(string argument)
    {
        if (!argument.StartsWith("--", System.StringComparison.InvariantCultureIgnoreCase))
        {
            return $"--{argument}";
        }

        return argument;
    }

    public IEnumerable<string> Arguments => _arguments;
}
