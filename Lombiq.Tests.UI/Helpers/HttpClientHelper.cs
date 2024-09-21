using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;

namespace Lombiq.Tests.UI.Helpers;

public static class HttpClientHelper
{
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposed by the returned client.")]
    public static HttpClient CreateCertificateIgnoringHttpClient(Uri? baseUri = null) =>
        new(CreateCertificateIgnoringHttpClientHandler()) { BaseAddress = baseUri };


    /// <summary>
    /// Creates a normally dangerous HTTP client handler that accepts any certificate, to allow working with self-signed
    /// development certificates.
    /// </summary>
    public static HttpClientHandler CreateCertificateIgnoringHttpClientHandler() =>
        new()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            // Revoked certificates shouldn't be used though.
            CheckCertificateRevocationList = true,
        };
}
