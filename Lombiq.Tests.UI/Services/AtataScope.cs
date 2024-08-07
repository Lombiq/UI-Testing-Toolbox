using Atata;
using OpenQA.Selenium;
using System;

namespace Lombiq.Tests.UI.Services;

/// <summary>
/// A representation of a scope wrapping an Atata-driven UI test. <see cref="AtataScope"/> is created in the beginning
/// of a UI test, provides services for the test, and is disposed when the test finishes.
/// </summary>
public sealed class AtataScope : IDisposable
{
    private Uri _baseUri;

    public AtataContext AtataContext { get; }

    public IWebDriver Driver
    {
        get
        {
            var driver = AtataContext.Driver;
            IsBrowserRunning = driver != null;
            return driver;
        }
    }

    public bool IsBrowserRunning { get; private set; }

    public Uri BaseUri
    {
        get => _baseUri;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _baseUri = value;
            AtataContext.BaseUrl = value.ToString();
        }
    }

    public AtataScope(AtataContext atataContext, Uri baseUri)
    {
        AtataContext = atataContext;
        _baseUri = baseUri;
    }

    /// <summary>
    /// Sets <see cref="AtataContext.Current"/> to the value of <see cref="AtataContext"/>.
    /// </summary>
    public void SetContextAsCurrent() => AtataContext.Current = AtataContext;

    public void Dispose() => AtataContext.Dispose();
}
