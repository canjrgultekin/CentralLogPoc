using CentralLogPoc.Shared.Constants;
using CentralLogPoc.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CentralLogPoc.InventoryService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InventoryController : ControllerBase
{
    // POC için sabit stok limiti
    private const int StockThreshold = 100;

    private readonly ILogger<InventoryController> _logger;

    public InventoryController(ILogger<InventoryController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Stok rezervasyonu endpoint'i.
    /// Quantity <= 100 ise rezervasyon başarılı; aksi halde yetersiz stok uyarısı.
    /// Tüm log satırları aynı CorrelationId + TraceId'i taşır.
    /// </summary>
    [HttpPost("reserve")]
    [ProducesResponseType(typeof(InventoryResult), StatusCodes.Status200OK)]
    public IActionResult Reserve([FromBody] ProcessRequest request)
    {
        var correlationId = HttpContext.Items[ObservabilityConstants.CorrelationIdHeader]
                                ?.ToString() ?? "-";

        var activity = Activity.Current;
        activity?.SetTag("inventory.product_id", request.ProductId);
        activity?.SetTag("inventory.requested_qty", request.Quantity);

        _logger.LogInformation(
            "[InventoryService] Rezervasyon isteği alındı. ProductId: {ProductId}, Qty: {Quantity}, CustomerId: {CustomerId}",
            request.ProductId, request.Quantity, request.CustomerId);

        // POC rezervasyon kuralı
        var isReserved = request.Quantity <= StockThreshold;

        activity?.SetTag("inventory.reserved", isReserved);

        if (isReserved)
        {
            _logger.LogInformation(
                "[InventoryService] Stok rezerve edildi. ProductId: {ProductId}, Qty: {Quantity}",
                request.ProductId, request.Quantity);
        }
        else
        {
            _logger.LogWarning(
                "[InventoryService] Yetersiz stok. ProductId: {ProductId}, " +
                "İstenen: {Quantity}, Limit: {StockThreshold}",
                request.ProductId, request.Quantity, StockThreshold);
        }

        // POC: hafif gecikme simüle et
        Thread.Sleep(Random.Shared.Next(10, 60));

        return Ok(new InventoryResult(isReserved));
    }
}
