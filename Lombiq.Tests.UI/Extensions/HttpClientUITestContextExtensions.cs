using Lombiq.Tests.UI.Services;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Extensions;

[SuppressMessage(
    "Security",
    "SCS0004: Certificate Validation has been disabled.",
    Justification = "Certificate validation is unnecessary for UI testing.")]
[SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "Disposed by the HttpClient.")]
public static class HttpClientUITestContextExtensions
{
    /// <summary>
    /// Creates a new <see cref="HttpClient"/> and authorizes it with a Bearer token that is created based on the provided
    /// <paramref name="clientId"/> and <paramref name="clientSecret"/>.
    /// </summary>
    public static async Task<HttpClient> CreateAndAuthorizeClientAsync(
        this UITestContext context,
        string clientId = "UITest",
        string clientSecret = "Password")
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            CheckCertificateRevocationList = true,
        };

        var client = new HttpClient(handler)
        {
            BaseAddress = context.Scope.BaseUri,
        };

        using var requestBody = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
        ]);

        var tokenUrl = context.Scope.BaseUri.AbsoluteUri + "connect/token";
        var tokenResponse = await client.PostAsync(tokenUrl, requestBody);

        var token = string.Empty;
        if (tokenResponse.IsSuccessStatusCode)
        {
            var responseContent = await tokenResponse.Content.ReadAsStringAsync();
            token = JObject.Parse(responseContent)["access_token"].ToString();
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>
    /// Issues a GET request to the given <paramref name="requestUri"/> using the provided <paramref name="client"/>.
    /// </summary>
    /// <returns>The response's <see cref="HttpContent"/> as a string.</returns>
    public static async Task<string> GetAndReadResponseContentAsync(
        this UITestContext context,
        HttpClient client,
        string requestUri)
    {
        var response = await client.GetAsync(requestUri);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Issues a POST request to the given <paramref name="requestUri"/> using the provided <paramref name="json"/> and
    /// <paramref name="client"/>.
    /// </summary>
    /// <returns>The response's <see cref="HttpContent"/> as a string.</returns>
    public static async Task<string> PostAndReadResponseContentAsync(
        this UITestContext context,
        HttpClient client,
        string requestUri,
        string json)
    {
        var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(requestUri, stringContent);

        return await response.Content.ReadAsStringAsync();
    }
}
