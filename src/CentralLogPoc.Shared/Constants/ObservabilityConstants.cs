namespace CentralLogPoc.Shared.Constants;

/// <summary>
/// Sabit isimler — header, baggage ve log property adları bir yerden yönetilir.
/// </summary>
public static class ObservabilityConstants
{
    /// <summary>HTTP istek/yanıt header adı.</summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>Serilog LogContext property adı.</summary>
    public const string CorrelationIdLogProperty = "CorrelationId";

    /// <summary>W3C Baggage key — OTel HttpClient instrumentation ile downstream'e otomatik propagate edilir.</summary>
    public const string CorrelationIdBaggageKey = "correlation.id";

    /// <summary>OTel Activity tag adı.</summary>
    public const string CorrelationIdTag = "correlation.id";
}
