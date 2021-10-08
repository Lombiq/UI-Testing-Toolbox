using Atata;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    public static class VerificationUITestContextExtensions
    {
        /// <summary>
        /// Adds a unique empty element to the page. It can be used to indicate if there has been any navigation since
        /// this method has been called.
        /// </summary>
        /// <returns>The query which indicates this unique element alone.</returns>
        public static By AddPageMarker(this UITestContext context)
        {
            var id = Guid.NewGuid().ToString("N");
            context.ExecuteScript($"var marker = document.createElement('DIV'); marker.id = '{id}'; document.body.appendChild(marker);");

            var by = By.Id(id).OfAnyVisibility();
            context.Exists(by);

            return by;
        }
    }
}
