#nullable enable
using AngleSharp.Html.Parser;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Lombiq.Tests.UI.Services;

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
