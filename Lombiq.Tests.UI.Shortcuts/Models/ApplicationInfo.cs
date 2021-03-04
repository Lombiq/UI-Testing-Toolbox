using System.Collections.Generic;

namespace Lombiq.Tests.UI.Shortcuts.Models
{
    public class ApplicationInfo
    {
        public string AppRoot { get; set; }
        public AssemblyInfo AssemblyInfo { get; set; }
        public IEnumerable<AssemblyInfo> Modules { get; set; }
    }

    public class AssemblyInfo
    {
        public string AssemblyName { get; set; }
        public string AssemblyLocation { get; set; }
    }
}
