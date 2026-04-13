using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using OrderApi.Common.Endpoints;
using OrderApi.Features.Orders.GetOrders;

namespace OrderApi.Features.Orders.GetOrders;

/// <summary>
/// Endpoint that returns a paginated list of all active (non-deleted) orders,
/// sorted newest-first. Results are output-cached under the "orders" tag so
/// mutating endpoints can invalidate them with a single eviction call.
/// </summary>
public sealed class GetOrdersEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/api/v1/orders", async (
            [AsParameters] PaginationQuery pagination,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (pagination.Validate() is { } validationError)
            {
                return Results.BadRequest(new { error = validationError });
            }

            return Results.Ok(await mediator.Send(
                new GetOrdersQuery(pagination.Page, pagination.PageSize), cancellationToken));
        })
        .WithName("GetOrders")
        .WithTags("Orders")
        .WithSummary("Get paginated list of all orders")
        .Produces<PagedResult<OrderSummaryResponse>>()
        .Produces(StatusCodes.Status400BadRequest)
        .RequireRateLimiting("fixed")
        .CacheOutput("orders");
    }
}
