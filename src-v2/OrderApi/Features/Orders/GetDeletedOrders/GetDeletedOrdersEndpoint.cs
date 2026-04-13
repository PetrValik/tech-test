using MediatR;
using OrderApi.Common.Endpoints;
using OrderApi.Features.Orders.GetOrders;

namespace OrderApi.Features.Orders.GetDeletedOrders;

/// <summary>
/// Endpoint that returns a paginated list of soft-deleted orders.
/// Deleted orders are excluded from all standard list endpoints;
/// this endpoint provides the audit trail for administrative review.
/// </summary>
public sealed class GetDeletedOrdersEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/api/v1/orders/deleted", async (
            [AsParameters] PaginationQuery pagination,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (pagination.Validate() is { } validationError)
            {
                return Results.BadRequest(new { error = validationError });
            }

            return Results.Ok(await mediator.Send(
                new GetDeletedOrdersQuery(pagination.Page, pagination.PageSize),
                cancellationToken));
        })
        .WithName("GetDeletedOrders")
        .WithTags("Orders")
        .WithSummary("Get paginated list of soft-deleted orders")
        .Produces<PagedResult<OrderSummaryResponse>>()
        .Produces(StatusCodes.Status400BadRequest)
        .RequireRateLimiting("fixed");
    }
}
