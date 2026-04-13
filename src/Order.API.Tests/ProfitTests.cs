using NUnit.Framework;
using Order.API.Tests.Helpers;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Order.API.Tests;

/// <summary>
/// Integration tests for GET /orders/profit/monthly.
/// Verifies correct profit aggregation by month and empty-result handling.
/// </summary>
[TestFixture]
public class ProfitTests : ApiTestBase
{
    /// <summary>
    /// GET /orders/profit/monthly returns correct grouped profit for Completed orders.
    /// </summary>
    [Test]
    public async Task GetMonthlyProfit_ReturnsProfitData()
    {
        // 2 units in Jan → profit = 2 * (0.9 - 0.8) = 0.2
        // 1 unit  in Jan → profit = 1 * (0.9 - 0.8) = 0.1  → Jan total = 0.3
        // 3 units in Feb → profit = 3 * (0.9 - 0.8) = 0.3
        await _factory.AddOrder(_seed, quantity: 2, statusId: _seed.StatusCompletedId, createdDate: new DateTime(2024, 1, 10));
        await _factory.AddOrder(_seed, quantity: 1, statusId: _seed.StatusCompletedId, createdDate: new DateTime(2024, 1, 20));
        await _factory.AddOrder(_seed, quantity: 3, statusId: _seed.StatusCompletedId, createdDate: new DateTime(2024, 2, 5));
        // Non-completed order should not appear
        await _factory.AddOrder(_seed);

        var response = await _client.GetAsync("/orders/profit/monthly");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var profits = await DeserializeAsync<List<MonthlyProfit>>(response);
        Assert.That(profits.Count, Is.EqualTo(2));

        Assert.That(profits[0].Year,  Is.EqualTo(2024));
        Assert.That(profits[0].Month, Is.EqualTo(1));
        Assert.That(Math.Round(profits[0].TotalProfit, 2), Is.EqualTo(0.3m));

        Assert.That(profits[1].Year,  Is.EqualTo(2024));
        Assert.That(profits[1].Month, Is.EqualTo(2));
        Assert.That(Math.Round(profits[1].TotalProfit, 2), Is.EqualTo(0.3m));
    }

    /// <summary>
    /// GET /orders/profit/monthly returns an empty list when there are no Completed orders.
    /// </summary>
    [Test]
    public async Task GetMonthlyProfit_ReturnsEmpty_WhenNoCompletedOrders()
    {
        await _factory.AddOrder(_seed);

        var response = await _client.GetAsync("/orders/profit/monthly");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var profits = await DeserializeAsync<List<MonthlyProfit>>(response);
        Assert.That(profits, Is.Empty);
    }
}
