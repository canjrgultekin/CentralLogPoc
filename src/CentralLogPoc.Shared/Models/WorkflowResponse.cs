namespace CentralLogPoc.Shared.Models;

/// <summary>API'den client'a dönen nihai yanıt. CorrelationId de eklendi — trace etmek için.</summary>
public sealed record WorkflowResponse(
    string OrderId,
    string ProductId,
    int Quantity,
    bool IsInventoryReserved,
    string CorrelationId);
