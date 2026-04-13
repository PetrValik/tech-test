using MediatR;
using OrderApi.Common.Endpoints;

namespace OrderApi.Features.Orders.GetOrderHistory;

/// <summary>
/// Endpoint that returns the paginated status-change audit trail for a specific order.
/// Each entry records the previous status, the new status, and the timestamp of the transition.
/// An order with no transitions returns an empty paged result.
/// </summary>
public sealed class GetOrderHistoryEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/api/v1/orders/{orderId:guid}/history", async (
            Guid orderId,
            [AsParameters] PaginationQuery pagination,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (pagination.Validate() is { } validationError)
            {
                return Results.BadRequest(new { error = validationError });
            }

            var history = await mediator.Send(
                new GetOrderHistoryQuery(orderId, pagination.Page, pagination.PageSize),
                cancellationToken);

            return Results.Ok(history);
        })
        .WithName("GetOrderHistory")
        .WithTags("Orders")
        .WithSummary("Get status change history for an order")
        .Produces<PagedResult<OrderStatusHistoryResponse>>()
        .Produces(StatusCodes.Status400BadRequest)
        .RequireRateLimiting("fixed");
    }
}
