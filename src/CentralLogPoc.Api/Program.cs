using CentralLogPoc.Api.Services;
using CentralLogPoc.Shared.Extensions;
using CentralLogPoc.Shared.Constants;
using CentralLogPoc.Shared.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Observability ─────────────────────────────────────────────────────────────
builder.AddSharedSerilog("CentralLogPoc.Api");
builder.AddSharedOpenTelemetry("CentralLogPoc.Api");

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

// CorrelationIdHandler her HttpClient isteğine X-Correlation-Id ekler
builder.Services.AddTransient<CorrelationIdHandler>();

builder.Services.AddHttpClient<GatewayClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["Services:Gateway"] ?? "http://localhost:5010");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<CorrelationIdHandler>();

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Middleware sırası önemli:
// 1. CorrelationId oluştur/oku — hem LogContext hem Activity set edilsin
// 2. Serilog request logging — böylece HTTP request log'u da CorrelationId taşır
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
        diagCtx.Set("UserAgent",
            httpCtx.Request.Headers.UserAgent.ToString());
    };
});

app.UseAuthorization();
app.MapControllers();
app.Run();
