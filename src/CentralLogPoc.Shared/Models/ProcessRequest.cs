namespace CentralLogPoc.Shared.Models;

/// <summary>API → Gateway ve Gateway → Microservices arası taşınan istek modeli.</summary>
public sealed record ProcessRequest(
    string ProductId,
    int Quantity,
    string CustomerId);
