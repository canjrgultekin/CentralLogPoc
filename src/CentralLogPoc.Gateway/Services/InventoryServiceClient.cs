using CentralLogPoc.Shared.Models;

namespace CentralLogPoc.Gateway.Services;

public sealed class InventoryServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryServiceClient> _logger;

    public InventoryServiceClient(HttpClient httpClient, ILogger<InventoryServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<InventoryResult> ReserveAsync(
        ProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Gateway→InventoryService] Stok rezervasyon isteği gönderiliyor. ProductId: {ProductId}, Qty: {Quantity}",
            request.ProductId, request.Quantity);

        var response = await _httpClient.PostAsJsonAsync(
            "/api/inventory/reserve", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "[Gateway→InventoryService] Hata. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<InventoryResult>(
                         cancellationToken: cancellationToken)
                     ?? throw new InvalidOperationException(
                         "InventoryService'ten null yanıt döndü.");

        _logger.LogInformation(
            "[Gateway←InventoryService] Stok durumu alındı. IsReserved: {IsReserved}",
            result.IsReserved);

        return result;
    }
}
