using System;
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
        _arguments.Add(FormattableString.Invariant($"{PrepareArg(key)}={value}"));

        return this;
    }

    [Obsolete("Use AddSwitch or AddWithValue instead.")]
    public InstanceCommandLineArgumentsBuilder Add(string value) => throw new NotSupportedException();

    private static string PrepareArg(string argument) =>
        $"--{argument.TrimStart('-')}";

    public IEnumerable<string> Arguments => _arguments;
}
