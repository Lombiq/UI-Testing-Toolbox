using System.Collections.Generic;

namespace Lombiq.Tests.UI.Models;

public class InstanceCommandLineArgs
{
    private readonly List<string> _args = new();

    public InstanceCommandLineArgs AddArg(string arg)
    {
        _args.Add(arg);
        return this;
    }

    public InstanceCommandLineArgs AddArgValue<T>(string arg, T value) =>
        AddArg($"{arg}={value}");

    public IEnumerable<string> Args => _args;
}
