using Lombiq.Tests.UI.Services;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Headers;
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
    public static async Task<HttpClient> CreateAndAuthorizeClientAsync(
        this UITestContext context,
        string clientId = null,
        string clientSecret = null)
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

        clientId ??= "UITest";
        clientSecret ??= "Password";
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

    public static async Task<string> GetResponseContentAsync(this UITestContext context, HttpClient client, string requestUri)
    {
        var response = await client.GetAsync(requestUri);
        return await response.Content.ReadAsStringAsync();
    }
}
