namespace Order.Model;

/// <summary>
/// Aggregated profit for a single calendar month across all Completed orders.
/// </summary>
public class MonthlyProfit
{
    /// <summary>
    /// Calendar year (e.g. 2024).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Calendar month, 1–12.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Sum of (UnitPrice − UnitCost) × Quantity for every item in Completed orders that month.
    /// </summary>
    public decimal TotalProfit { get; set; }
}
