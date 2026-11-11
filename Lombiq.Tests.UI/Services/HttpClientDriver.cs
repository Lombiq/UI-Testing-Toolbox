#nullable enable

using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using Lombiq.Tests.UI.Helpers;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Services;

public sealed class HttpClientDriver : IWebDriver
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
        Document == null ? null : new HttpClientWebElement(this, Document.DocumentElement).FindElement(by);
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member.

    public ReadOnlyCollection<IWebElement> FindElements(By by) =>
        Document == null ? [] : new HttpClientWebElement(this, Document!.DocumentElement).FindElements(by);

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

    public ITargetLocator SwitchTo() => throw new NotSupportedException();

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

public class HttpClientDriverNavigation : INavigation
{
    private readonly Stack<Uri> _history = new();
    private readonly HttpClientDriver _driver;

    public HttpClientDriverNavigation(HttpClientDriver driver) => _driver = driver;

    public void Back() => GoToUrlAsync(_history.Pop(), addToHistory: false).GetAwaiter().GetResult();
    public Task BackAsync() => GoToUrlAsync(_history.Pop(), addToHistory: false);
    public void Forward() => throw new NotSupportedException();
    public Task ForwardAsync() => throw new NotSupportedException();
    public void Refresh() => GoToUrl(_driver.Url);
    public Task RefreshAsync() => GoToUrlAsync(_driver.Url);
    public void GoToUrl(string url) => GoToUrl(new Uri(_driver.BaseUri, url));
    public void GoToUrl(Uri url) => GoToUrlAsync(url).GetAwaiter().GetResult();
    public Task GoToUrlAsync(string url) => GoToUrlAsync(new Uri(_driver.BaseUri, url));
    public Task GoToUrlAsync(Uri url) => GoToUrlAsync(url, !url.AbsoluteUri.EqualsOrdinalIgnoreCase(_driver.Url));

    public async Task GoToUrlAsync(Uri url, bool addToHistory)
    {
        _driver._url = url.AbsoluteUri;

        var token = TestContext.Current.CancellationToken;

        using var response = await _driver.Client.GetAsync(url, token);
        await response.ThrowIfNotSuccessAsync($"Failed to GET the URL \"{url}\".");
        await using var stream = await response.Content.ReadAsStreamAsync(token);
        _driver.Document = await new HtmlParser().ParseDocumentAsync(stream, token);

        if (addToHistory) _history.Push(url);
    }
}

public class HttpClientWebElement : IWebElement
{
    private readonly HttpClientDriver _driver;
    private readonly IElement _element;

    public string TagName => _element.TagName;
    public string Text => _element.TextContent;
    public bool Enabled => _element.IsEnabled();
    public bool Selected => !string.IsNullOrEmpty(_element.GetAttribute("selected"));
    public Point Location => default;
    public Size Size => default;
    public bool Displayed => string.IsNullOrEmpty(_element.GetAttribute("hidden"));

    public HttpClientWebElement(HttpClientDriver driver, IElement element)
    {
        _driver = driver;
        _element = element;
    }

    // Seems to be a mistake in the parent type, because it is possible to get null if the element is not found.
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member.
    public IWebElement? FindElement(By by) => FindElements(by).FirstOrDefault();
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member.

    public ReadOnlyCollection<IWebElement> FindElements(By by)
    {
        // CssSelectorMechanism: "css selector"
        // XPathSelectorMechanism: "xpath"
        // TagNameMechanism: "tag name"
        // LinkTextMechanism: "link text"
        // PartialLinkTextMechanism: "partial link text"

        static ReadOnlyCollection<IWebElement> Cast(HttpClientDriver driver, IEnumerable<IElement> elements) =>
            new(elements.Select(element => new HttpClientWebElement(driver, element)).Cast<IWebElement>().ToList());

        return Cast(_driver, by.Mechanism switch
        {
            "css selector" => _element.QuerySelectorAll(by.Criteria),
            "xpath" => _element.SelectNodes(by.Criteria).CastWhere<IElement>(),
            "tag name" => _element.GetElementsByTagName(by.Criteria),
            "link text" => _element.GetElementsByTagName(TagNames.A).Where(a => a.TextContent.Trim() == by.Criteria),
            "partial link text" => _element.GetElementsByTagName(TagNames.A).Where(a => a.TextContent.Contains(by.Criteria)),
            _ => throw new InvalidOperationException($"Unknown mechanism \"{by.Mechanism}\"."),
        });
    }

    public void Clear() => throw new NotSupportedException();
    public void SendKeys(string text) => throw new NotSupportedException();
    public void Submit() => throw new NotSupportedException();

    public void Click()
    {
        var element = _element;
        while (element != null && element.TagName != TagNames.A)
        {
            element = element.ParentElement;
        }

        if (element?.GetAttribute("href") is { } href)
        {
            _driver.Url = href;
        }
    }

    public string? GetAttribute(string attributeName) => _element.GetAttribute(attributeName);
    public string? GetDomAttribute(string attributeName) => _element.GetAttribute(attributeName);
    public string? GetDomProperty(string propertyName) => throw new NotSupportedException();
    public string GetCssValue(string propertyName) => throw new NotSupportedException();
    public ISearchContext GetShadowRoot() => throw new NotSupportedException();
}
