using Lombiq.HelpfulLibraries.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Shortcuts.Middlewares;

public class ExceptionContextLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionContextLoggingMiddleware(RequestDelegate next) => _next = next;

    [SuppressMessage(
        "Minor Code Smell",
        "S6667:Logging in a catch clause should pass the caught exception as a parameter.",
        Justification = "This middleware provides additional information beyond what's already logged, which " +
                        "includes the exception, so that would be redundant.")]
    public async Task InvokeAsync(HttpContext context)
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<ExceptionContextLoggingMiddleware>>();
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(
                "HTTP request when the exception \"{ExceptionMessage}\" happened:\n{HttpContext}",
                exception.Message,
                HttpRequestInfo.ToJson(context.Request));
            throw;
        }
    }
}
