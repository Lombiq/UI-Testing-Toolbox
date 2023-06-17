using Lombiq.Tests.UI.Services;
using System.Net.Http;
using System.Threading.Tasks;

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

    public static async Task<T> FetchWithBrowserContextAsync<T>(
        this UITestContext context,
        HttpMethod method,
        string address,
        Func<HttpResponseMessage, Task<T>> processResponseAsync)
    {
        var searchUrl = new Uri(new Uri(context.Driver.Url), address);

        // Certificate checking is not necessary because the request URL is relative to where the browser already is.
#pragma warning disable CA5399 // HttpClient is created without enabling CheckCertificateRevocationList
        using var handler = new HttpClientHandler { CookieContainer = context.GetCookieContainer() };
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(method, searchUrl);
        using var response = await client.SendAsync(request);
#pragma warning restore CA5399

        return await processResponseAsync(response);
    }
}
