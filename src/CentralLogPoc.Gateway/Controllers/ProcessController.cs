using CentralLogPoc.Gateway.Services;
using CentralLogPoc.Shared.Constants;
using CentralLogPoc.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace CentralLogPoc.Gateway.Controllers;

[ApiController]
[Route("gateway")]
public sealed class ProcessController : ControllerBase
{
    private readonly OrderServiceClient _orderClient;
    private readonly InventoryServiceClient _inventoryClient;
    private readonly ILogger<ProcessController> _logger;

    public ProcessController(
        OrderServiceClient orderClient,
        InventoryServiceClient inventoryClient,
        ILogger<ProcessController> logger)
    {
        _orderClient = orderClient;
        _inventoryClient = inventoryClient;
        _logger = logger;
    }

    /// <summary>
    /// Gateway'in tek görevi: OrderService ve InventoryService'i paralel çağırıp sonuçları toplamak.
    /// Her iki çağrıda da aynı CorrelationId ve aynı TraceId taşınır.
    /// OTel bu noktada iki ayrı child span yaratır; ikisi de aynı trace'in altındadır.
    /// </summary>
    [HttpPost("process")]
    [ProducesResponseType(typeof(ProcessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Process(
        [FromBody] ProcessRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Items[ObservabilityConstants.CorrelationIdHeader]
                                ?.ToString() ?? "-";

        _logger.LogInformation(
            "[Gateway] İstek alındı. ProductId: {ProductId}, Qty: {Quantity}, CustomerId: {CustomerId}",
            request.ProductId, request.Quantity, request.CustomerId);

        // Paralel çağrı — aynı anda her iki mikroservis de tetiklenir
        // OTel her biri için ayrı child span açar; her ikisi de aynı TraceId'i taşır
        var orderTask     = _orderClient.CreateOrderAsync(request, cancellationToken);
        var inventoryTask = _inventoryClient.ReserveAsync(request, cancellationToken);

        _logger.LogDebug(
            "[Gateway] OrderService ve InventoryService paralel olarak çağrılıyor...");

        await Task.WhenAll(orderTask, inventoryTask);

        var orderResult     = orderTask.Result;
        var inventoryResult = inventoryTask.Result;

        _logger.LogInformation(
            "[Gateway] Her iki servis yanıtladı. OrderId: {OrderId}, InventoryReserved: {IsReserved}",
            orderResult.OrderId, inventoryResult.IsReserved);

        var response = new ProcessResponse(
            OrderId: orderResult.OrderId,
            ProductId: request.ProductId,
            Quantity: request.Quantity,
            IsInventoryReserved: inventoryResult.IsReserved,
            CorrelationId: correlationId);

        return Ok(response);
    }
}
