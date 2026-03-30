using CentralLogPoc.Api.Services;
using CentralLogPoc.Shared.Constants;
using CentralLogPoc.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace CentralLogPoc.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WorkflowController : ControllerBase
{
    private readonly GatewayClient _gatewayClient;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(GatewayClient gatewayClient, ILogger<WorkflowController> logger)
    {
        _gatewayClient = gatewayClient;
        _logger = logger;
    }

    /// <summary>
    /// Distributed trace'in başladığı nokta.
    /// CorrelationId burada set edilmiş olur (middleware yaptı);
    /// bu noktadan itibaren tüm log satırları ve tüm downstream servisler aynı ID'yi taşır.
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(WorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartWorkflow(
        [FromBody] WorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Items[ObservabilityConstants.CorrelationIdHeader]
                                ?.ToString() ?? "-";

        _logger.LogInformation(
            "[API] Workflow başlatıldı. ProductId: {ProductId}, Qty: {Quantity}, CustomerId: {CustomerId}",
            request.ProductId, request.Quantity, request.CustomerId);

        var processRequest = new ProcessRequest(
            ProductId: request.ProductId,
            Quantity: request.Quantity,
            CustomerId: request.CustomerId);

        var processResponse = await _gatewayClient.ProcessAsync(processRequest, cancellationToken);

        var response = new WorkflowResponse(
            OrderId: processResponse.OrderId,
            ProductId: processResponse.ProductId,
            Quantity: processResponse.Quantity,
            IsInventoryReserved: processResponse.IsInventoryReserved,
            CorrelationId: correlationId);

        _logger.LogInformation(
            "[API] Workflow tamamlandı. OrderId: {OrderId}, InventoryReserved: {IsInventoryReserved}",
            response.OrderId, response.IsInventoryReserved);

        return Ok(response);
    }
}
