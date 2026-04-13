using MediatR;
using OrderApi.Common.Endpoints;
using OrderApi.Features.Orders.GetOrders;

namespace OrderApi.Features.Orders.GetOrdersByStatus;

/// <summary>
/// Endpoint that returns a paginated, output-cached list of orders whose status
/// matches the statusName route parameter. The comparison is case-insensitive;
/// an unrecognised status name returns an empty result rather than 404.
/// </summary>
public sealed class GetOrdersByStatusEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/api/v1/orders/status/{statusName}", async (
            string statusName,
            [AsParameters] PaginationQuery pagination,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            if (pagination.Validate() is { } validationError)
            {
                return Results.BadRequest(new { error = validationError });
            }

            return Results.Ok(await mediator.Send(
                new GetOrdersByStatusQuery(statusName, pagination.Page, pagination.PageSize),
                cancellationToken));
        })
        .WithName("GetOrdersByStatus")
        .WithTags("Orders")
        .WithSummary("Get orders filtered by status name")
        .Produces<PagedResult<OrderSummaryResponse>>()
        .Produces(StatusCodes.Status400BadRequest)
        .RequireRateLimiting("fixed")
        .CacheOutput("orders");
    }
}
