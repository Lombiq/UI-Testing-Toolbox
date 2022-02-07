using Lombiq.Tests.UI.Services;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Models
{
    public class UITestManifest
    {
        public string Name { get; set; }
        public Func<UITestContext, Task> TestAsync { get; set; }
    }
}
