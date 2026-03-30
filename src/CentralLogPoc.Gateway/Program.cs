using CentralLogPoc.Gateway.Services;
using CentralLogPoc.Shared.Extensions;
using CentralLogPoc.Shared.Constants;
using CentralLogPoc.Shared.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Observability ─────────────────────────────────────────────────────────────
builder.AddSharedSerilog("CentralLogPoc.Gateway");
builder.AddSharedOpenTelemetry("CentralLogPoc.Gateway");

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<CorrelationIdHandler>();

// OrderService typed client
builder.Services.AddHttpClient<OrderServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:OrderService"] ?? "http://localhost:5020");
    client.Timeout = TimeSpan.FromSeconds(15);
})
.AddHttpMessageHandler<CorrelationIdHandler>();

// InventoryService typed client
builder.Services.AddHttpClient<InventoryServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:InventoryService"] ?? "http://localhost:5030");
    client.Timeout = TimeSpan.FromSeconds(15);
})
.AddHttpMessageHandler<CorrelationIdHandler>();

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCorrelationId();

app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
    {
        diagCtx.Set("CorrelationId",
            httpCtx.Items[ObservabilityConstants.CorrelationIdHeader]?.ToString() ?? "-");
        diagCtx.Set("RequestHost", httpCtx.Request.Host.Value);
    };
});

app.UseAuthorization();
app.MapControllers();
app.Run();
