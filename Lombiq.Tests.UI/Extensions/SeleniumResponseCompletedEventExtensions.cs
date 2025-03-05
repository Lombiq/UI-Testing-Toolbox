using Lombiq.HelpfulLibraries.Common.Utilities;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium.BiDi.Modules.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lombiq.Tests.UI.Extensions;

public static class SeleniumResponseCompletedEventExtensions
{
    public static string ToFormattedString(this IEnumerable<ResponseData> responses) =>
        string.Join(Environment.NewLine, responses.Select(ToFormattedString));

    public static string ToFormattedString(this ResponseData response) =>
         StringHelper.CreateInvariant(
             $"URL: {response.Url}{Environment.NewLine}" +
             $"Status: {response.Status}{Environment.NewLine}" +
             $"Headers: {string.Join(", ", response.Headers.Select(header => $"{header.Name}: {header.Value}"))}{Environment.NewLine}" +
             $"Mime type: {response.MimeType}{Environment.NewLine}" +
             $"Bytes received: {response.BytesReceived}{Environment.NewLine}" +
             $"Headers size: {response.HeadersSize}{Environment.NewLine}" +
             $"Body size: {response.BodySize}{Environment.NewLine}" +
             $"Response content: {response.Content} {Environment.NewLine}");

    public static bool IsNonSuccessResponse(this ResponseCompletedEventArgs eventArgs) =>
        OrchardCoreUITestExecutorConfiguration.IsNonSuccessResponse(eventArgs);

    public static bool IsNonSuccessResponseAndNotExpectedNotFoundResponse(this ResponseCompletedEventArgs eventArgs, string urlContains) =>
        IsNonSuccessResponse(eventArgs) &&
        !eventArgs.IsNotFoundResponse(urlContains);

    public static bool IsNonSuccessResponseAndNotExpectedStatusResponse(this ResponseCompletedEventArgs eventArgs, string urlContains, int status) =>
        IsNonSuccessResponse(eventArgs) &&
        !(eventArgs.Response.Url.ContainsOrdinalIgnoreCase(urlContains) && eventArgs.Response.Status == status);

    public static bool IsNotFoundResponse(this ResponseCompletedEventArgs eventArgs, string urlContains) =>
        IsNotFoundResponse(eventArgs.Response, urlContains);

    public static bool IsNotFoundResponse(this ResponseData response, string urlContains) =>
        response.Status == 404 && response.Url.ContainsOrdinalIgnoreCase(urlContains);
}
