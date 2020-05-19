using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Attributes
{
    public sealed class EdgeAttribute : BrowserAttributeBase
    {
        protected override Browser Browser => Browser.Edge;
    }
}
