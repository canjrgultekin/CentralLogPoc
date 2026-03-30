using CentralLogPoc.Shared.Middleware;
using Microsoft.AspNetCore.Builder;

namespace CentralLogPoc.Shared.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// CorrelationIdMiddleware'i pipeline'a ekler.
    /// UseSerilogRequestLogging'den ÖNCE çağrılmalı ki request log'larında da CorrelationId görünsün.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
