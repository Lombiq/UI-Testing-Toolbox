using Lombiq.Tests.UI.Services;
using OpenQA.Selenium.BiDi.Modules.Network;
using System;
using System.Collections.Generic;

namespace Lombiq.Tests.UI.Extensions;

public static class SeleniumResponseCompletedEventExtensions
{
    public static string ToFormattedString(this IEnumerable<ResponseData> responses) =>
        string.Join(Environment.NewLine, responses);

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
