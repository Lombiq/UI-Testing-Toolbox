using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Attributes
{
    public sealed class FirefoxAttribute : BrowserAttributeBase
    {
        protected override Browser Browser => Browser.Firefox;
    }
}
