using CentralLogPoc.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace CentralLogPoc.Shared.Middleware;

/// <summary>
/// Gelen istekten X-Correlation-Id okur; yoksa yeni üretir.
/// Aynı değeri:
///   1. HttpContext.Items'a koyar (CorrelationIdHandler bu değeri okur)
///   2. Serilog LogContext'e push eder (request süresince tüm loglar CorrelationId taşır)
///   3. Activity.Current tag + baggage'a yazar (OTel downstream propagation)
///   4. Response header'a ekler
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[ObservabilityConstants.CorrelationIdHeader]
                                .FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");

        // HttpContext.Items — aynı istek içinde CorrelationIdHandler buradan okur
        context.Items[ObservabilityConstants.CorrelationIdHeader] = correlationId;

        // Response header — client de görebilsin
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(ObservabilityConstants.CorrelationIdHeader))
                context.Response.Headers[ObservabilityConstants.CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // OTel Activity — tag + baggage ile downstream'e taşınır
        var activity = Activity.Current;
        activity?.SetTag(ObservabilityConstants.CorrelationIdTag, correlationId);
        activity?.SetBaggage(ObservabilityConstants.CorrelationIdBaggageKey, correlationId);

        // Serilog LogContext — bu using bloğu içindeki tüm log event'ler CorrelationId taşır
        using (LogContext.PushProperty(ObservabilityConstants.CorrelationIdLogProperty, correlationId))
        {
            await _next(context);
        }
    }
}
