using MediatR;
using OrderApi.Common.Endpoints;

namespace OrderApi.Features.Orders.GetOrderById;

/// <summary>
/// Endpoint that retrieves the full details of a single order by its GUID.
/// The response includes an ETag header containing the order's concurrency stamp,
/// which callers must supply as the If-Match header on subsequent update requests
/// to prevent lost-update conflicts.
/// </summary>
public sealed class GetOrderByIdEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/api/v1/orders/{orderId:guid}", async (
            Guid orderId,
            IMediator mediator,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var order = await mediator.Send(new GetOrderByIdQuery(orderId), cancellationToken);
            if (order is null)
            {
                return Results.NotFound();
            }

            httpContext.Response.Headers.ETag = $"\"{order.ConcurrencyStamp}\"";
            return Results.Ok(order);
        })
        .WithName("GetOrderById")
        .WithTags("Orders")
        .WithSummary("Get order details by ID")
        .Produces<OrderDetailResponse>()
        .Produces(StatusCodes.Status404NotFound)
        .RequireRateLimiting("fixed")
        .CacheOutput("orders");
    }
}
