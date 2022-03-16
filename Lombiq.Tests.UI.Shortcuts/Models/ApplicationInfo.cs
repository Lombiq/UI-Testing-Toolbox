using System.Collections.Generic;
using System.Diagnostics;

namespace Lombiq.Tests.UI.Shortcuts.Models;

public class ApplicationInfo
{
    public string AppRoot { get; set; }
    public AssemblyInfo AssemblyInfo { get; set; }
    public IEnumerable<ModuleInfo> Modules { get; set; }
}

[DebuggerDisplay("{AssemblyName}")]
public class AssemblyInfo
{
    public string AssemblyName { get; set; }
    public string AssemblyLocation { get; set; }
}

public class ModuleInfo : AssemblyInfo
{
    public IEnumerable<string> Assets { get; set; }
}
