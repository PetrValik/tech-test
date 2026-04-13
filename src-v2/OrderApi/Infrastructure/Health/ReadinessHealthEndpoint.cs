using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrderApi.Common.Endpoints;

namespace OrderApi.Infrastructure.Health;

/// <summary>
/// Endpoint for the Kubernetes readiness probe at GET /health/ready.
/// Runs all registered health checks (currently the database check) and returns
/// 200 OK when all checks pass, or 503 Service Unavailable when any check fails.
/// A 503 response causes Kubernetes to stop routing traffic to this pod until
/// the dependency recovers.
/// </summary>
public sealed class ReadinessHealthEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            ResponseWriter = async (httpContext, report) =>
            {
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString()
                    })
                });
            }
        });
    }
}
