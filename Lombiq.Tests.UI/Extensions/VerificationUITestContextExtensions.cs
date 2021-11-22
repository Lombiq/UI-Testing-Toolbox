using Lombiq.Tests.UI.Models;
using Lombiq.Tests.UI.Services;

namespace Lombiq.Tests.UI.Extensions
{
    public static class VerificationUITestContextExtensions
    {
        /// <summary>
        /// Returns a <see cref="PageNavigationState"/> of the current page in the <paramref name="context"/>.
        /// </summary>
        public static PageNavigationState AsPageNavigationState(this UITestContext context) => new(context);
    }
}
