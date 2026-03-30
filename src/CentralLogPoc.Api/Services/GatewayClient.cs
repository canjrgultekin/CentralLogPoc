using CentralLogPoc.Shared.Models;

namespace CentralLogPoc.Api.Services;

public sealed class GatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GatewayClient> _logger;

    public GatewayClient(HttpClient httpClient, ILogger<GatewayClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProcessResponse> ProcessAsync(
        ProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[API→Gateway] İstek gönderiliyor. ProductId: {ProductId}, Qty: {Quantity}",
            request.ProductId, request.Quantity);

        var response = await _httpClient.PostAsJsonAsync(
            "/gateway/process", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "[API→Gateway] Hata yanıtı. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode(); // throw
        }

        var result = await response.Content.ReadFromJsonAsync<ProcessResponse>(
                         cancellationToken: cancellationToken)
                     ?? throw new InvalidOperationException(
                         "Gateway servisinden null yanıt döndü.");

        _logger.LogInformation(
            "[API←Gateway] Yanıt alındı. OrderId: {OrderId}, InventoryReserved: {IsInventoryReserved}",
            result.OrderId, result.IsInventoryReserved);

        return result;
    }
}
