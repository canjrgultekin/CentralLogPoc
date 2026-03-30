using CentralLogPoc.Shared.Extensions;
using CentralLogPoc.Shared.Constants;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedSerilog("CentralLogPoc.OrderService");
builder.AddSharedOpenTelemetry("CentralLogPoc.OrderService");

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

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
    };
});

app.UseAuthorization();
app.MapControllers();
app.Run();
