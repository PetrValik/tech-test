using MediatR;

namespace OrderApi.Features.Orders.GetProfitByMonth;

/// <summary>
/// MediatR query to fetch monthly profit figures.
/// </summary>
public record GetProfitByMonthQuery() : IRequest<IEnumerable<MonthlyProfitResponse>>;
