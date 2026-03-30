using CentralLogPoc.Shared.Constants;
using CentralLogPoc.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CentralLogPoc.OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ILogger<OrdersController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Bu noktada:
    ///   - CorrelationId: Gateway'den gelen X-Correlation-Id header'ından CorrelationIdMiddleware okudu
    ///   - TraceId: OTel W3C traceparent propagation ile aynı root trace'i taşıyor
    ///   - SpanId: Bu servis için yeni bir child span — Gateway span'ının altında
    /// Seq'de filtrelerken: CorrelationId = "xxx" yazsan bu log da görünür.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResult), StatusCodes.Status201Created)]
    public IActionResult CreateOrder([FromBody] ProcessRequest request)
    {
        var correlationId = HttpContext.Items[ObservabilityConstants.CorrelationIdHeader]
                                ?.ToString() ?? "-";

        // Mevcut Activity'i zenginleştir
        var activity = Activity.Current;
        activity?.SetTag("order.product_id", request.ProductId);
        activity?.SetTag("order.quantity",   request.Quantity);
        activity?.SetTag("order.customer_id", request.CustomerId);

        var orderId = $"ORD-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        _logger.LogInformation(
            "[OrderService] Sipariş oluşturuldu. OrderId: {OrderId}, ProductId: {ProductId}, " +
            "Qty: {Quantity}, CustomerId: {CustomerId}",
            orderId, request.ProductId, request.Quantity, request.CustomerId);

        activity?.SetTag("order.id", orderId);

        // POC: hafif gecikme simüle et
        Thread.Sleep(Random.Shared.Next(10, 50));

        return StatusCode(StatusCodes.Status201Created, new OrderResult(orderId));
    }
}
