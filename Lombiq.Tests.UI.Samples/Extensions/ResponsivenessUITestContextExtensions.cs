using Lombiq.Tests.UI.Constants;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Samples.Extensions
{
    public static class ResponsivenessUITestContextExtensions
    {
        public static void SetStandardBrowserSize(this UITestContext context) =>
            context.SetBrowserSize(CommonDisplayResolutions.Fhd);
    }
}
