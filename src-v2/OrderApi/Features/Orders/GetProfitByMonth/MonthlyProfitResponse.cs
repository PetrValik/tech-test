namespace OrderApi.Features.Orders.GetProfitByMonth;

/// <summary>
/// Monthly profit summary for completed orders.
/// </summary>
public record MonthlyProfitResponse(int Year, int Month, decimal TotalProfit);
