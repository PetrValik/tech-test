using MediatR;
using OrderApi.Common.Endpoints;
using OrderApi.Features.Orders.GetOrders;

namespace OrderApi.Features.Orders.SearchOrders;

/// <summary>
/// Endpoint that searches orders with any combination of optional filters:
/// date range, reseller, customer, status name, and total price bounds.
/// When no filters are supplied the result is equivalent to GET /orders.
/// Applies the "expensive" rate-limiting policy because the query may perform
/// full table scans with price aggregations.
/// Results are output-cached under the "orders" tag.
/// </summary>
public sealed class SearchOrdersEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/api/v1/orders/search", async (
            DateTime? from,
            DateTime? to,
            Guid? resellerId,
            Guid? customerId,
            string? status,
            decimal? minTotal,
            decimal? maxTotal,
            [AsParameters] PaginationQuery pagination,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (pagination.Validate() is { } validationError)
            {
                return Results.BadRequest(new { error = validationError });
            }

            return Results.Ok(await mediator.Send(
                new SearchOrdersQuery(
                    from, to, resellerId, customerId, status,
                    minTotal, maxTotal, pagination.Page, pagination.PageSize),
                cancellationToken));
        })
        .WithName("SearchOrders")
        .WithTags("Orders")
        .WithSummary("Search orders with optional filters")
        .Produces<PagedResult<OrderSummaryResponse>>()
        .Produces(StatusCodes.Status400BadRequest)
        .RequireRateLimiting("expensive")
        .CacheOutput("orders");
    }
}
