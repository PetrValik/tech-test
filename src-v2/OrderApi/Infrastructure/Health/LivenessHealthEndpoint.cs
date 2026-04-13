using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrderApi.Common.Endpoints;

namespace OrderApi.Infrastructure.Health;

/// <summary>
/// Endpoint for the Kubernetes liveness probe at GET /health/live.
/// Always returns 200 OK as long as the process is alive — no dependency checks
/// are performed so the pod is never removed from the load balancer due to a
/// transient database outage. A non-200 response here causes Kubernetes to
/// restart the container.
/// </summary>
public sealed class LivenessHealthEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            // Skip all registered health checks — liveness only cares that the
            // process is running and can handle HTTP requests.
            Predicate = _ => false,
            ResponseWriter = async (httpContext, _) =>
            {
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsJsonAsync(new { status = "Healthy" });
            }
        });
    }
}
