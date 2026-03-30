using CentralLogPoc.Shared.Constants;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace CentralLogPoc.Shared.Middleware;

/// <summary>
/// HttpClient DelegatingHandler — her outgoing HTTP isteğine X-Correlation-Id ekler.
/// Öncelik sırası: HttpContext.Items → Activity Baggage → yeni GUID.
/// OTel instrumentation zaten W3C traceparent ekliyor; bu handler business-level ID'yi ekler.
/// </summary>
public sealed class CorrelationIdHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId =
            _httpContextAccessor.HttpContext
                ?.Items[ObservabilityConstants.CorrelationIdHeader]
                ?.ToString()
            ?? Activity.Current?.GetBaggageItem(ObservabilityConstants.CorrelationIdBaggageKey)
            ?? Guid.NewGuid().ToString("N");

        if (!request.Headers.Contains(ObservabilityConstants.CorrelationIdHeader))
            request.Headers.TryAddWithoutValidation(
                ObservabilityConstants.CorrelationIdHeader, correlationId);

        return base.SendAsync(request, cancellationToken);
    }
}
