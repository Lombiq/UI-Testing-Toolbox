#nullable enable
using AngleSharp.Dom;
using AngleSharp.XPath;
using Atata;
using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace Lombiq.Tests.UI.Services;

public class HttpClientWebElement : IWebElement, IExtendedSearchContext
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

        // Workaround for https://github.com/atata-framework/atata-webdriverextras/issues/62.
        var byType = by.GetType();
        if (byType.Name == "ExtendedBy" &&
            byType.GetProperty("By", BindingFlags.NonPublic | BindingFlags.Instance)?.GetMethod?.Invoke(by, []) is By realBy)
        {
            by = realBy;
        }

        // Evaluate ByChain step-by-step.
        if (by is ByChain byChain)
        {
            var collection = new List<IWebElement> { this };

            foreach (var byItem in byChain.Items)
            {
                collection = collection
                    .SelectMany(oldElement => oldElement.FindElements(byItem))
                    .ToList();
            }

            return new(collection);
        }

        return Cast(_driver, by.Mechanism switch
        {
            "css selector" => _element.QuerySelectorAll(by.Criteria),
            "xpath" => _element.SelectNodes(by.Criteria).CastWhere<IElement>(),
            "tag name" => _element.GetElementsByTagName(by.Criteria),
            "link text" => _element.GetElementsByTagName(TagNames.A).Where(a => a.TextContent.Trim() == by.Criteria),
            "partial link text" => _element.GetElementsByTagName(TagNames.A).Where(a => a.TextContent.Contains(by.Criteria)),
            _ => throw new InvalidOperationException($"{by.GetType().FullName} has an unknown mechanism: \"{by.Mechanism}\""),
        });
    }

    public bool Exists(By by) => FindElements(by).Count > 0;

    public bool Missing(By by) => !Exists(by);

    public void Clear() => throw new NotSupportedException();
    public void SendKeys(string text) => throw new NotSupportedException();
    public void Submit() => throw new NotSupportedException();

    public void Click()
    {
        var element = _element;
        while (element != null && !TagNames.A.EqualsOrdinalIgnoreCase(element.TagName))
        {
            element = element.ParentElement;
        }

        if (element?.GetAttribute("href") is { } href)
        {
            _driver.Navigate().GoToUrl(href);
        }
    }

    public string? GetAttribute(string attributeName) => _element.GetAttribute(attributeName);
    public string? GetDomAttribute(string attributeName) => _element.GetAttribute(attributeName);
    public string? GetDomProperty(string propertyName) => throw new NotSupportedException();
    public string GetCssValue(string propertyName) => throw new NotSupportedException();
    public ISearchContext GetShadowRoot() => throw new NotSupportedException();
}
