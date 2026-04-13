using NUnit.Framework;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Order.Service.Tests;

/// <summary>
/// Unit/integration tests for <see cref="IOrderService.GetMonthlyProfitAsync"/> using a real
/// SQLite in-memory database to exercise the repository and service layers together.
/// </summary>
[TestFixture]
public class ProfitServiceTests : ServiceTestBase
{
    [Test]
    public async Task GetMonthlyProfitAsync_ReturnsEmpty_WhenNoCompletedOrders()
    {
        // Arrange - add a Created (not Completed) order
        await AddOrder(Guid.NewGuid(), 1, _orderStatusCreatedId);

        // Act
        var result = await _orderService.GetMonthlyProfitAsync();

        // Assert
        Assert.That(result.Any(), Is.False);
    }

    [Test]
    public async Task GetMonthlyProfitAsync_ReturnsCorrectProfit()
    {
        // Arrange - add a completed order with quantity 2; profit = 2 * (0.9 - 0.8) = 0.2
        var orderId = Guid.NewGuid();
        await AddOrder(orderId, 2, _orderStatusCompletedId);

        // Act
        var result = await _orderService.GetMonthlyProfitAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(Math.Round(result.First().TotalProfit, 2), Is.EqualTo(0.2m));
    }
}
