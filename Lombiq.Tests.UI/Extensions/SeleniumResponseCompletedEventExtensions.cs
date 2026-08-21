using Lombiq.Tests.UI.Services;
using OpenQA.Selenium.BiDi.Network;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;

namespace Lombiq.Tests.UI.Extensions;

public static class SeleniumResponseCompletedEventExtensions
{
    public static string ToFormattedString(this IEnumerable<ResponseData> responses) =>
        string.Join(Environment.NewLine, responses.Select(ToFormattedString));

    public static string ToFormattedString(this ResponseData response) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"URL: {response.Url}{Environment.NewLine}" +
            $"Status: {response.Status}{Environment.NewLine}" +
            $"Headers: {string.Join(", ", response.Headers.Select(header => $"{header.Name}: {header.Value}"))}{Environment.NewLine}" +
            $"Mime type: {response.MimeType}{Environment.NewLine}" +
            $"Bytes received: {response.BytesReceived}{Environment.NewLine}" +
            $"Headers size: {response.HeadersSize}{Environment.NewLine}" +
            $"Body size: {response.BodySize}{Environment.NewLine}" +
            $"Response content: {response.Content} {Environment.NewLine}");

    public static void WithIgnoreExpectedNotFoundResponseFilter(
        this OrchardCoreUITestExecutorConfiguration configuration,
        string urlContains) =>
        configuration.WithIgnoreExpectedStatusResponseFilter(urlContains, HttpStatusCode.NotFound);

    public static void WithIgnoreExpectedStatusResponseFilter(
        this OrchardCoreUITestExecutorConfiguration configuration,
        string urlContains,
        HttpStatusCode status) =>
        configuration.WithIgnoreExpectedStatusResponseFilter(urlContains, (int)status);

    public static void WithIgnoreExpectedStatusResponseFilter(
        this OrchardCoreUITestExecutorConfiguration configuration,
        string urlContains,
        int status) =>
        configuration.ResponseLogFilters[$"Ignore expected {status.ToTechnicalString()} error at {urlContains}."] =
            eventArgs => !(eventArgs.Response.Url.ContainsOrdinalIgnoreCase(urlContains) && eventArgs.Response.Status == status);
}
