#nullable enable
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Services;

public class HttpClientDriverTargetLocator : ITargetLocator
{
    public HttpClientDriver Driver { get; }

    public HttpClientDriverTargetLocator(HttpClientDriver driver) =>
        Driver = driver;

    public IWebDriver Frame(int frameIndex) => Driver;
    public IWebDriver Frame(string frameName) => Driver;
    public IWebDriver Frame(IWebElement frameElement) => Driver;
    public IWebDriver ParentFrame() => Driver;
    public IWebDriver Window(string windowName) => Driver;
    public IWebDriver NewWindow(WindowType typeHint) => throw new NotSupportedException("This driver is single-window only.");
    public IWebDriver DefaultContent() => Driver;
    public IWebElement ActiveElement() =>
        Driver.Document is { } document
            ? new HttpClientWebElement(Driver, document.DocumentElement)
            : null!;
    public IAlert Alert() => throw new NoAlertPresentException("This driver cannot show alerts.");
}
