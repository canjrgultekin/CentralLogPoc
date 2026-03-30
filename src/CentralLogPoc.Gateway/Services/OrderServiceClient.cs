using CentralLogPoc.Shared.Models;

namespace CentralLogPoc.Gateway.Services;

public sealed class OrderServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderServiceClient> _logger;

    public OrderServiceClient(HttpClient httpClient, ILogger<OrderServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OrderResult> CreateOrderAsync(
        ProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[Gateway→OrderService] Sipariş oluşturma isteği gönderiliyor. ProductId: {ProductId}",
            request.ProductId);

        var response = await _httpClient.PostAsJsonAsync(
            "/api/orders", request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "[Gateway→OrderService] Hata. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        var result = await response.Content.ReadFromJsonAsync<OrderResult>(
                         cancellationToken: cancellationToken)
                     ?? throw new InvalidOperationException(
                         "OrderService'ten null yanıt döndü.");

        _logger.LogInformation(
            "[Gateway←OrderService] Sipariş oluşturuldu. OrderId: {OrderId}",
            result.OrderId);

        return result;
    }
}
