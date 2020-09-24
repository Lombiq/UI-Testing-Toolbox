using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Attributes
{
    public sealed class InternetExplorerAttribute : BrowserAttributeBase
    {
        protected override Browser Browser => Browser.InternetExplorer;
    }
}
