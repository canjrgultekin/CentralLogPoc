namespace CentralLogPoc.Shared.Models;

/// <summary>Gateway'den API'ye dönen yanıt modeli.</summary>
public sealed record ProcessResponse(
    string OrderId,
    string ProductId,
    int Quantity,
    bool IsInventoryReserved,
    string CorrelationId);
