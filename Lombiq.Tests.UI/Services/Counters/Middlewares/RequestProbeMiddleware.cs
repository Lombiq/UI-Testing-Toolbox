using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services.Counters.Middlewares;

public class RequestProbeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICounterDataCollector _counterDataCollector;

    public RequestProbeMiddleware(RequestDelegate next, ICounterDataCollector counterDataCollector)
    {
        _next = next;
        _counterDataCollector = counterDataCollector;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using (new RequestProbe(_counterDataCollector, context.Request.Method, new Uri(context.Request.GetEncodedUrl())))
            await _next.Invoke(context);
    }
}
