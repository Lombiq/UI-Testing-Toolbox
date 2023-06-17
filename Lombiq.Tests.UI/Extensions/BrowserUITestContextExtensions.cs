using Lombiq.Tests.UI.Services;
using System.Net.Http;

namespace System.Net;

public static class BrowserUITestContextExtensions
{
    /// <summary>
    /// Gets all cookies from the browser and converts them into .NET <see cref="Cookie"/> instances in a <see
    /// cref="CookieContainer"/>. This can be useful if you want to make web requests using <see cref="HttpClient"/>
    /// while using the login and other cookies from the browser.
    /// </summary>
    public static CookieContainer GetCookieContainer(this UITestContext context)
    {
        var cookieContainer = new CookieContainer();
        foreach (var seleniumCookie in context.Driver.Manage().Cookies.AllCookies)
        {
            var netCookie = new Cookie
            {
                Domain = seleniumCookie.Domain,
                HttpOnly = seleniumCookie.IsHttpOnly,
                Name = seleniumCookie.Name,
                Path = seleniumCookie.Path,
                Secure = seleniumCookie.Secure,
                Value = seleniumCookie.Value,
            };

            if (seleniumCookie.Expiry.HasValue) netCookie.Expires = seleniumCookie.Expiry.Value;

            cookieContainer.Add(netCookie);
        }

        return cookieContainer;
    }
}
