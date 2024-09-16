using Lombiq.Tests.UI.Services;
using Newtonsoft.Json.Linq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    public static HttpClient CreateClient(this UITestContext context)
    {

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            CheckCertificateRevocationList = true,
        };

        return new(handler) { BaseAddress = context.Scope.BaseUri };
    }

    /// <summary>
    /// Creates a new <see cref="HttpClient"/> and authorizes it with a Bearer token that is created based on the provided
    /// parameters.
    /// </summary>
    public static async Task<HttpClient> CreateAndAuthorizeClientAsync(
        this UITestContext context,
        string grantType = "client_credentials",
        string clientId = "UITest",
        string clientSecret = "Password",
        string userName = null,
        string password = null)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("grant_type", grantType),
            new("client_id", clientId),
            new("client_secret", clientSecret),
        };

        if (string.Equals(grantType, "password", StringComparison.OrdinalIgnoreCase))
        {
            parameters.Add(new KeyValuePair<string, string>("username", userName));
            parameters.Add(new KeyValuePair<string, string>("password", password));
        }

        using var requestBody = new FormUrlEncodedContent(parameters);

        var client = context.CreateClient();
        var tokenUrl = new Uri(context.Scope.BaseUri, "connect/token");
        using var tokenResponse = await client.PostAsync(tokenUrl, requestBody);

        await tokenResponse.ThrowIfNotSuccessAsync(
            $"Failed to get token for user in {nameof(CreateAndAuthorizeClientAsync)}.", requestBody);

        var responseContent = await tokenResponse.Content.ReadAsStringAsync();
        var token = JsonNode.Parse(responseContent)?["access_token"]?.ToString();
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
    /// Issues a GET request to the given <paramref name="requestUri"/> using the provided <paramref name="client"/> then
    /// deserializes the response content to the given <typeparamref name="TObject"/>.
    /// </summary>
    /// <returns>The response's <see cref="HttpContent"/> as a string.</returns>
    public static async Task<TObject> GetAndReadResponseContentAsync<TObject>(
        this UITestContext context,
        HttpClient client,
        string requestUri)
        where TObject : class
    {
        var content = await GetAndReadResponseContentAsync(context, client, requestUri);
        var parsed = JToken.Parse(content);
        return parsed.ToObject<TObject>();
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
        using var response = await PostAndGetResponseAsync(client, requestUri, json);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Issues a POST request to the given <paramref name="requestUri"/> using the provided <paramref name="json"/> then
    /// deserializes the response content to the given <typeparamref name="TObject"/>.
    /// </summary>
    /// <returns>The deserialized <typeparamref name="TObject"/> object.</returns>
    public static async Task<TObject> PostAndReadResponseContentAsync<TObject>(
        this UITestContext context,
        HttpClient client,
        string requestUri,
        string json)
        where TObject : class
    {
        using var response = await PostAndGetResponseAsync(client, requestUri, json);
        var content = await response.Content.ReadAsStringAsync();
        var parsed = JToken.Parse(content);

        return parsed.ToObject<TObject>();
    }

    /// <summary>
    /// Issues a POST request to the given <paramref name="requestUri"/> using the provided
    /// <paramref name="objectToSerialize"/> which will be serialized as json.
    /// </summary>
    /// <returns>The response's <see cref="HttpContent"/> as a string.</returns>
    public static Task<string> PostAndReadResponseContentAsync(
        this UITestContext context,
        HttpClient client,
        object objectToSerialize,
        string requestUri)
    {
        var json = JsonSerializer.Serialize(objectToSerialize);
        return PostAndReadResponseContentAsync(context, client, requestUri, json);
    }

    /// <summary>
    /// Issues a POST request to the given <paramref name="requestUri"/> using the provided
    /// <paramref name="objectToSerialize"/> which will be serialized as json.
    /// </summary>
    /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
    public static Task<HttpResponseMessage> PostAndGetResponseAsync(
        this UITestContext context,
        HttpClient client,
        object objectToSerialize,
        string requestUri)
    {
        var json = JsonSerializer.Serialize(objectToSerialize);
        return PostAndGetResponseAsync(client, requestUri, json);
    }

    /// <summary>
    /// Issues a POST request to the given <paramref name="requestUri"/> using the provided <paramref name="json"/>.
    /// </summary>
    /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
    public static async Task<HttpResponseMessage> PostAndGetResponseAsync(
        HttpClient client,
        string requestUri,
        string json)
    {
        var stringContent = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        var response = await client.PostAsync(requestUri, stringContent);

        return response;
    }

    public static async Task PostAndResponseStatusCodeShouldBeAsync(
        this UITestContext context,
        HttpClient client,
        object objectToSerialize,
        string requestUri,
        HttpStatusCode expected)
    {
        using var response = await context.PostAndGetResponseAsync(client, objectToSerialize, requestUri);
        response.StatusCode.ShouldBe(expected);
    }
}
