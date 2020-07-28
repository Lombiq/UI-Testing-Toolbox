using Atata;
using OpenQA.Selenium.Remote;
using System;

namespace Lombiq.Tests.UI.Services
{
    /// <summary>
    /// A representation of a scope wrapping an Atata-driven UI test. <see cref="AtataScope"/> is created in the
    /// beginning of a UI test, provides services for the test, and is disposed when the test finishes.
    /// </summary>
    public sealed class AtataScope : IDisposable
    {
        public AtataContext AtataContext { get; }
        public RemoteWebDriver Driver => AtataContext.Driver;
        public Uri BaseUri { get; }


        public AtataScope(AtataContext atataContext, Uri baseUri)
        {
            AtataContext = atataContext;
            BaseUri = baseUri;
        }


        public void Dispose() => AtataContext.Dispose();
    }
}
