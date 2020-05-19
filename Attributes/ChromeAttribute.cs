using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Attributes
{
    public sealed class ChromeAttribute : BrowserAttributeBase
    {
        protected override Browser Browser => Browser.Chrome;
    }
}
