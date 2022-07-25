using System.Collections.Generic;

namespace Lombiq.Tests.UI.Models;

public class InstanceCommandLineArgs
{
    private readonly List<string> _args = new();

    public InstanceCommandLineArgs AddSwitch(string arg)
    {
        _args.Add($"--{arg}");

        return this;
    }

    public InstanceCommandLineArgs AddValue<T>(string arg, T value)
    {
        _args.Add($"--{arg}={value}");

        return this;
    }

    public IEnumerable<string> Args => _args;
}
