using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using OrderApi.Common.Endpoints;

namespace OrderApi.Features.Orders.CreateOrder;

/// <summary>
/// Endpoint that creates a new order from the JSON request body.
/// Validates product IDs against the database before persisting; returns the
/// new order's location URL in the Location header on success.
/// Sends an Idempotency-Key-aware response when the header is present,
/// preventing duplicate orders on retried requests.
/// </summary>
public sealed class CreateOrderEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/api/v1/orders", async (
            CreateOrderCommand? command,
            IMediator mediator,
            IOutputCacheStore cacheStore,
            CancellationToken cancellationToken) =>
        {
            if (command is null)
            {
                return Results.BadRequest(new { error = "Request body is required." });
            }

            var result = await mediator.Send(command, cancellationToken);
            if (!result.Success)
            {
                return Results.BadRequest(new
                {
                    error = "One or more product IDs are invalid.",
                    invalidProductIds = result.InvalidProductIds
                });
            }

            await cacheStore.EvictByTagAsync("orders", cancellationToken);
            return Results.Created($"/api/v1/orders/{result.OrderId}", new { id = result.OrderId });
        })
        .WithName("CreateOrder")
        .WithTags("Orders")
        .WithSummary("Create a new order")
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .RequireRateLimiting("fixed");
    }
}
