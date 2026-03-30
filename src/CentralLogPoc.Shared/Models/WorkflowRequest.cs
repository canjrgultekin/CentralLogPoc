namespace CentralLogPoc.Shared.Models;

/// <summary>API entry point'e gelen istek modeli.</summary>
public sealed record WorkflowRequest(
    string ProductId,
    int Quantity,
    string CustomerId);
