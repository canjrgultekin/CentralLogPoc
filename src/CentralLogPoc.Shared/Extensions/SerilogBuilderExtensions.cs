using CentralLogPoc.Shared.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Serilog;

namespace CentralLogPoc.Shared.Extensions;

public static class SerilogBuilderExtensions
{
    /// <summary>
    /// Konsol output template: her log satırında servis adı, CorrelationId, TraceId ve SpanId görünür.
    /// Örnek çıktı:
    ///   [12:34:56.789 INF] [CentralLogPoc.Gateway] [CorId:a3f8...] [Trace:1234abcd...] [Span:ef56...] Workflow started
    /// </summary>
    private const string ConsoleOutputTemplate =
        "[{Timestamp:HH:mm:ss.fff} {Level:u3}] " +
        "[{ServiceName}] " +
        "[CorId:{CorrelationId}] " +
        "[Trace:{TraceId}] " +
        "[Span:{SpanId}] " +
        "{Message:lj}" +
        "{NewLine}{Exception}";

    public static WebApplicationBuilder AddSharedSerilog(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        builder.Host.UseSerilog((context, services, loggerConfig) =>
        {
            var seqUrl = context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341";

            loggerConfig
                .ReadFrom.Configuration(context.Configuration)   // appsettings MinimumLevel vs.
                .ReadFrom.Services(services)                      // DI entegrasyonu
                .Enrich.FromLogContext()                           // PushProperty scope'ları
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.With<ActivityEnricher>()                  // TraceId + SpanId + CorrelationId fallback
                .Enrich.WithProperty("ServiceName", serviceName)  // hangi servis logladı
                .WriteTo.Console(outputTemplate: ConsoleOutputTemplate)
                .WriteTo.Seq(seqUrl);
        });

        return builder;
    }
}
