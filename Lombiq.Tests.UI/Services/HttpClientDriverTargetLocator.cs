#nullable enable
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Services;

public class HttpClientDriverTargetLocator : ITargetLocator
{
    private readonly HttpClientDriver _driver;

    public HttpClientDriverTargetLocator(HttpClientDriver driver) =>
        _driver = driver;

    public IWebDriver Frame(int frameIndex) => _driver;
    public IWebDriver Frame(string frameName) => _driver;
    public IWebDriver Frame(IWebElement frameElement) => _driver;
    public IWebDriver ParentFrame() => _driver;
    public IWebDriver Window(string windowName) => _driver;
    public IWebDriver NewWindow(WindowType typeHint) => throw new NotSupportedException("This driver is single-window only.");
    public IWebDriver DefaultContent() => _driver;
    public IWebElement ActiveElement() =>
        _driver.Document is { } document
            ? new HttpClientWebElement(_driver, document.DocumentElement)
            : null!;
    public IAlert Alert() => throw new NoAlertPresentException("This driver cannot show alerts.");
}
