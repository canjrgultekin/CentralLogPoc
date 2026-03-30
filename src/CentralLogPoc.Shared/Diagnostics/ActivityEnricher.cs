using CentralLogPoc.Shared.Constants;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace CentralLogPoc.Shared.Diagnostics;

/// <summary>
/// Her log event'e System.Diagnostics.Activity.Current üzerinden TraceId ve SpanId ekler.
/// CorrelationId ise W3C Baggage üzerinden fallback olarak okunur;
/// esas olarak CorrelationIdMiddleware LogContext üzerinden zaten set eder.
/// </summary>
public sealed class ActivityEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));

        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));

        // Baggage'dan CorrelationId — LogContext zaten set ettiyse AddPropertyIfAbsent override etmez.
        var correlationId = activity.GetBaggageItem(ObservabilityConstants.CorrelationIdBaggageKey);
        if (correlationId is not null)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty(
                    ObservabilityConstants.CorrelationIdLogProperty, correlationId));
        }
    }
}
