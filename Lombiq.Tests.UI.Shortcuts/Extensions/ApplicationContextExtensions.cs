using Lombiq.Tests.UI.Shortcuts.Models;
using System.Linq;

namespace OrchardCore.Modules
{
    public static class ApplicationContextExtensions
    {
        public static ApplicationInfo GetApplicationInfo(this IApplicationContext applicationContext)
        {
            var application = applicationContext.Application;

            return new ApplicationInfo
            {
                AppRoot = application.Root,
                AssemblyInfo = new AssemblyInfo
                {
                    AssemblyLocation = application.Assembly.Location,
                    AssemblyName = application.Assembly.ToString(),
                },
                Modules = application.Modules.Select(
                    module => new ModuleInfo
                    {
                        AssemblyLocation = module.Assembly.Location,
                        AssemblyName = module.Assembly.ToString(),
                        Assets = module.Assets.Select(asset => asset.ProjectAssetPath),
                    }),
            };
        }
    }
}
