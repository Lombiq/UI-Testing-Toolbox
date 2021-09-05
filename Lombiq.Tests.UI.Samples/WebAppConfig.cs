using System;
using System.IO;

namespace Lombiq.Tests.UI.Samples
{
    // We somehow need to tell the UI Testing Toolbox where the assemblies of the app under test is (since it'll run the
    // app from the command line). This class
    public static class WebAppConfig
    {
        public static string GetAbsoluteApplicationAssemblyPath()
        {
            // The test assembly can be in a folder below the src and test folders (those should be in the repo root).
            var baseDirectory = File.Exists("Lombiq.OSOCE.Web.dll")
                ? AppContext.BaseDirectory
                : Path.Combine(
                    AppContext.BaseDirectory.Split(new[] { "src", "test" }, StringSplitOptions.RemoveEmptyEntries)[0],
                    "src",
                    "Lombiq.OSOCE.Web",
                    "bin",
                    "Debug",
                    "netcoreapp3.1");

            return Path.Combine(baseDirectory, "Lombiq.OSOCE.Web.dll");
        }
    }
}

// NEXT STATION: Let's configure how tests should work. Head over to UITestBase.cs.
