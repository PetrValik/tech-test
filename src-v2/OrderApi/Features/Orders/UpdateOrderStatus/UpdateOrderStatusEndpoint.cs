using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using OrderApi.Common.Endpoints;

namespace OrderApi.Features.Orders.UpdateOrderStatus;

/// <summary>
/// Endpoint that transitions an order to a new status.
/// Supports optimistic concurrency: when the caller supplies an If-Match header
/// containing the order's current ETag, the update is rejected with 409 Conflict if
/// the order has been modified by another request since the ETag was retrieved.
/// Evicts the "orders" output cache tag on success.
/// </summary>
public sealed class UpdateOrderStatusEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPatch("/api/v1/orders/{orderId:guid}/status", async (
            Guid orderId,
            UpdateOrderStatusRequest? body,
            IMediator mediator,
            IOutputCacheStore cacheStore,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (body is null)
            {
                return Results.BadRequest(new { error = "Request body is required." });
            }

            var ifMatchHeader = httpContext.Request.Headers.IfMatch.FirstOrDefault()?.Trim('"');

            var result = await mediator.Send(
                new UpdateOrderStatusCommand(orderId, body.StatusName, ifMatchHeader),
                cancellationToken);

            if (result == UpdateResult.Success)
            {
                await cacheStore.EvictByTagAsync("orders", cancellationToken);
            }

            return result switch
            {
                UpdateResult.Success       => Results.NoContent(),
                UpdateResult.OrderNotFound => Results.NotFound(),
                UpdateResult.InvalidStatus => Results.BadRequest(new { error = $"Invalid status: {body.StatusName}" }),
                UpdateResult.Conflict      => Results.Conflict(new { error = "The order has been modified. Refresh and retry." }),
                _                          => Results.StatusCode(StatusCodes.Status500InternalServerError)
            };
        })
        .WithName("UpdateOrderStatus")
        .WithTags("Orders")
        .WithSummary("Update an order's status")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .RequireRateLimiting("fixed");
    }
}
