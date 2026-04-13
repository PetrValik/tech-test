using MediatR;
using OrderApi.Common.Endpoints;

namespace OrderApi.Features.Orders.GetProfitByMonth;

/// <summary>
/// Endpoint that returns a list of monthly profit totals aggregated across all
/// completed orders. Applies the "expensive" rate-limiting policy because the
/// underlying query performs a full table scan with aggregations.
/// Results are output-cached under the "orders" tag.
/// </summary>
public sealed class GetProfitByMonthEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/api/v1/orders/profit/monthly", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
                Results.Ok(await mediator.Send(new GetProfitByMonthQuery(), cancellationToken)))
        .WithName("GetMonthlyProfit")
        .WithTags("Orders")
        .WithSummary("Get monthly profit for completed orders")
        .Produces<IEnumerable<MonthlyProfitResponse>>()
        .RequireRateLimiting("expensive")
        .CacheOutput("orders");
    }
}
