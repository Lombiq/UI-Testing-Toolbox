using Lombiq.Tests.UI.Services;
using System;

namespace Lombiq.Tests.UI.Models
{
    public class UITestManifest
    {
        public string Name { get; set; }
        public Action<UITestContext> Test { get; set; }
    }
}
