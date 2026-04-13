using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderApi.Infrastructure;

namespace OrderApi.Features.Orders.GetProfitByMonth;

/// <summary>
/// Returns monthly profit figures aggregated from all orders with status "Completed".
/// </summary>
/// <param name="orderContext">The database context used to query completed order items.</param>
public sealed class GetProfitByMonthHandler(OrderContext orderContext)
    : IRequestHandler<GetProfitByMonthQuery, IEnumerable<MonthlyProfitResponse>>
{
    private const int ProfitLookbackMonths = 24;
    /// <summary>
    /// Loads completed order items and groups them by year and month.
    /// Uses server-side aggregation for MySQL and falls back to client-side aggregation for SQLite tests.
    /// <para>
    /// Overflow safety: decimal arithmetic always throws <see cref="OverflowException"/> on overflow.
    /// With validated inputs (quantity ≤ 1 000 000, price ≤ 999 999.9999), the max per-item value is ~10^12,
    /// well within decimal.MaxValue (~7.9 × 10^28).
    /// </para>
    /// </summary>
    /// <param name="query">The query object (currently carries no filter parameters).</param>
    /// <param name="cancellationToken">Token used to cancel the database query.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a chronologically sorted sequence of <see cref="MonthlyProfitResponse"/> records.</returns>
    public async Task<IEnumerable<MonthlyProfitResponse>> Handle(GetProfitByMonthQuery query, CancellationToken cancellationToken)
    {
        if (orderContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
        {
            return await orderContext.OrderItems
                .AsNoTracking()
                .Where(item => item.Order.Status.Name == OrderStatusNames.Completed)
                .Where(item => item.Order.CreatedDate >= DateTime.UtcNow.AddMonths(-ProfitLookbackMonths))
                .GroupBy(item => new { item.Order.CreatedDate.Year, item.Order.CreatedDate.Month })
                .Select(group => new MonthlyProfitResponse(
                    group.Key.Year,
                    group.Key.Month,
                    group.Sum(item => (decimal)(item.Quantity ?? 0) * (item.Product.UnitPrice - item.Product.UnitCost))))
                .OrderBy(profit => profit.Year)
                .ThenBy(profit => profit.Month)
                .ToListAsync(cancellationToken);
        }

        var items = await orderContext.OrderItems
            .AsNoTracking()
            .Where(item => item.Order.Status.Name == OrderStatusNames.Completed)
            .Where(item => item.Order.CreatedDate >= DateTime.UtcNow.AddMonths(-ProfitLookbackMonths))
            .Include(item => item.Product)
            .Include(item => item.Order)
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(item => new { item.Order.CreatedDate.Year, item.Order.CreatedDate.Month })
            .Select(group => new MonthlyProfitResponse(
                group.Key.Year,
                group.Key.Month,
                group.Sum(item => (decimal)(item.Quantity ?? 0) * (item.Product.UnitPrice - item.Product.UnitCost))))
            .OrderBy(profit => profit.Year).ThenBy(profit => profit.Month)
            .ToList();
    }
}
