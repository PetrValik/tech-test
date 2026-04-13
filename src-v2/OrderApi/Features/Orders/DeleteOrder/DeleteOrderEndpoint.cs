using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using OrderApi.Common.Endpoints;

namespace OrderApi.Features.Orders.DeleteOrder;

/// <summary>
/// Endpoint that soft-deletes an order by its GUID.
/// The order is hidden from all standard list and detail endpoints after deletion
/// but remains retrievable via the dedicated GET /orders/deleted endpoint.
/// Evicts the "orders" output cache tag on success.
/// </summary>
public sealed class DeleteOrderEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapDelete("/api/v1/orders/{orderId:guid}", async (
            Guid orderId,
            IMediator mediator,
            IOutputCacheStore cacheStore,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new DeleteOrderCommand(orderId), cancellationToken);
            if (result == DeleteOrderResult.Deleted)
            {
                await cacheStore.EvictByTagAsync("orders", cancellationToken);
            }

            return result == DeleteOrderResult.Deleted
                ? Results.NoContent()
                : Results.NotFound();
        })
        .WithName("DeleteOrder")
        .WithTags("Orders")
        .WithSummary("Soft-delete an order")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .RequireRateLimiting("fixed");
    }
}
