#nullable enable

using AngleSharp.Html.Dom;
using Atata;
using Lombiq.Tests.UI.Helpers;
using OpenQA.Selenium;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services;

public sealed class HttpClientDriver : IWebDriver, IExtendedSearchContext
{
    public HttpClient Client { get; }
    internal string _url = "about:blank";
    public IHtmlDocument? Document { get; internal set; }

    public string Url
    {
        get => _url;
        set
        {
            _url = value;
            Navigate().GoToUrl(value);
        }
    }

    public BrowserConfiguration Configuration { get; }
    public Uri BaseUri => Client.BaseAddress ?? field;

    // Inherited properties.
    public string Title => Document?.QuerySelector("title")?.TextContent.Trim() ?? string.Empty;
    public string PageSource => Document?.DocumentElement.InnerHtml ?? string.Empty;
    public string CurrentWindowHandle => nameof(HttpClientDriver);
    public ReadOnlyCollection<string> WindowHandles { get; } = [nameof(HttpClientDriver)];
    public bool IsActionExecutor => true;

    public HttpClientDriver(BrowserConfiguration configuration, TimeSpan pageLoadTimeout, Uri baseUri)
    {
        Configuration = configuration;
        BaseUri = baseUri;
        Client = HttpClientHelper.CreateCertificateIgnoringHttpClient(baseUri);
        Client.Timeout = pageLoadTimeout;
    }

    // Seems to be a mistake in the parent type, because it is possible to get null if the element is not found.
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member.
    public IWebElement? FindElement(By by) =>
        Document?.DocumentElement is { } element ? new HttpClientWebElement(this, element).FindElement(by) : null;
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member.

    public ReadOnlyCollection<IWebElement> FindElements(By by) =>
        Document?.DocumentElement is { } element ? new HttpClientWebElement(this, element).FindElements(by) : [];

    public bool Exists(By by) =>
        Document?.DocumentElement is { } element && new HttpClientWebElement(this, element).Exists(by);

    public bool Missing(By by) => !Exists(by);

    public void Close()
    {
        // Not really a browser.
    }

    public void Quit()
    {
        // Not really a browser.
    }

    public IOptions Manage() => throw new NotSupportedException();

    public INavigation Navigate() => new HttpClientDriverNavigation(this);

    public ITargetLocator SwitchTo() => new HttpClientDriverTargetLocator(this);

    public void Dispose()
    {
        Client.Dispose();
        Document?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await CastAndDispose(Client);
        if (Document != null) await CastAndDispose(Document);

        return;

        static async ValueTask CastAndDispose(IDisposable resource)
        {
            if (resource is IAsyncDisposable resourceAsyncDisposable) await resourceAsyncDisposable.DisposeAsync();
            else resource.Dispose();
        }
    }
}
