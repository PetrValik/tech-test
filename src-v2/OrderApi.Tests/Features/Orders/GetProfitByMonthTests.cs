using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderApi.Features.Orders.GetProfitByMonth;
using OrderApi.Infrastructure;
using OrderApi.Infrastructure.Entities;
using OrderApi.Tests.Common;
using System.Net.Http.Json;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Integration tests for GET /orders/profit/monthly — monthly profit aggregation.
/// </summary>
[Collection("Orders")]
public sealed class GetProfitByMonthTests : OrdersEndpointTestBase
{
    /// <summary>
    /// Initialises the test class with the shared application factory.
    /// </summary>
    /// <param name="factory">The <see cref="OrderApiTestFactory"/> fixture injected by xUnit.</param>
    public GetProfitByMonthTests(OrderApiTestFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that GET /orders/profit/monthly returns a non-empty list of monthly profits
    /// with correctly computed total profit for completed orders.
    /// </summary>
    [Fact]
    public async Task GetProfitByMonth_ReturnsMonthlyProfit()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, _, productId, serviceId) = await SeedReferenceDataAsync(db);

        var completedStatus = await db.OrderStatuses.FirstAsync(s => s.Name == "Completed");
        await SeedOrderAsync(db, new Guid(completedStatus.Id), productId, serviceId, quantity: 2);

        var response = await Client.GetAsync("/api/v1/orders/profit/monthly");
        response.EnsureSuccessStatusCode();

        var profits = await response.Content.ReadFromJsonAsync<List<MonthlyProfitResponse>>();
        Assert.NotNull(profits);
        Assert.NotEmpty(profits);
        Assert.Equal(0.2m, Math.Round(profits.First().TotalProfit, 1));
    }

    /// <summary>
    /// Verifies that GET /orders/profit/monthly returns an empty list when there are no completed orders.
    /// </summary>
    [Fact]
    public async Task GetProfitByMonth_ReturnsEmpty_WhenNoCompletedOrders()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        var response = await Client.GetAsync("/api/v1/orders/profit/monthly");
        response.EnsureSuccessStatusCode();

        var profits = await response.Content.ReadFromJsonAsync<List<MonthlyProfitResponse>>();
        Assert.NotNull(profits);
        Assert.Empty(profits);
    }

    /// <summary>
    /// Verifies that GET /orders/profit/monthly groups results by month — two orders in different months
    /// produce two separate profit entries, sorted chronologically.
    /// </summary>
    [Fact]
    public async Task GetProfitByMonth_GroupsByMonth()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, _, productId, serviceId) = await SeedReferenceDataAsync(db);

        var completedStatus = await db.OrderStatuses.FirstAsync(s => s.Name == "Completed");

        var janOrderId = Guid.NewGuid();
        db.Orders.Add(new Infrastructure.Entities.Order
        {
            Id = janOrderId.ToByteArray(),
            ResellerId = Guid.NewGuid().ToByteArray(),
            CustomerId = Guid.NewGuid().ToByteArray(),
            StatusId = completedStatus.Id,
            CreatedDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        });
        db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid().ToByteArray(),
            OrderId = janOrderId.ToByteArray(),
            ProductId = productId.ToByteArray(),
            ServiceId = serviceId.ToByteArray(),
            Quantity = 2
        });

        var marOrderId = Guid.NewGuid();
        db.Orders.Add(new Infrastructure.Entities.Order
        {
            Id = marOrderId.ToByteArray(),
            ResellerId = Guid.NewGuid().ToByteArray(),
            CustomerId = Guid.NewGuid().ToByteArray(),
            StatusId = completedStatus.Id,
            CreatedDate = new DateTime(2025, 3, 10, 0, 0, 0, DateTimeKind.Utc)
        });
        db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid().ToByteArray(),
            OrderId = marOrderId.ToByteArray(),
            ProductId = productId.ToByteArray(),
            ServiceId = serviceId.ToByteArray(),
            Quantity = 5
        });

        await db.SaveChangesAsync();

        var response = await Client.GetAsync("/api/v1/orders/profit/monthly");
        response.EnsureSuccessStatusCode();

        var profits = await response.Content.ReadFromJsonAsync<List<MonthlyProfitResponse>>();
        Assert.NotNull(profits);
        Assert.Equal(2, profits.Count);
        Assert.Equal(1, profits[0].Month);
        Assert.Equal(3, profits[1].Month);
    }

    /// <summary>
    /// Verifies that GET /orders/profit/monthly sums profits from multiple completed orders that fall in the same month
    /// into a single entry.
    /// </summary>
    [Fact]
    public async Task GetProfitByMonth_SumsMultipleOrdersInSameMonth()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, _, productId, serviceId) = await SeedReferenceDataAsync(db);

        var completedStatus = await db.OrderStatuses.FirstAsync(s => s.Name == "Completed");

        var orderAId = Guid.NewGuid();
        db.Orders.Add(new Infrastructure.Entities.Order
        {
            Id = orderAId.ToByteArray(),
            ResellerId = Guid.NewGuid().ToByteArray(),
            CustomerId = Guid.NewGuid().ToByteArray(),
            StatusId = completedStatus.Id,
            CreatedDate = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc)
        });
        db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid().ToByteArray(),
            OrderId = orderAId.ToByteArray(),
            ProductId = productId.ToByteArray(),
            ServiceId = serviceId.ToByteArray(),
            Quantity = 2
        });

        var orderBId = Guid.NewGuid();
        db.Orders.Add(new Infrastructure.Entities.Order
        {
            Id = orderBId.ToByteArray(),
            ResellerId = Guid.NewGuid().ToByteArray(),
            CustomerId = Guid.NewGuid().ToByteArray(),
            StatusId = completedStatus.Id,
            CreatedDate = new DateTime(2025, 1, 25, 0, 0, 0, DateTimeKind.Utc)
        });
        db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid().ToByteArray(),
            OrderId = orderBId.ToByteArray(),
            ProductId = productId.ToByteArray(),
            ServiceId = serviceId.ToByteArray(),
            Quantity = 3
        });

        await db.SaveChangesAsync();

        var response = await Client.GetAsync("/api/v1/orders/profit/monthly");
        response.EnsureSuccessStatusCode();

        var profits = await response.Content.ReadFromJsonAsync<List<MonthlyProfitResponse>>();
        Assert.NotNull(profits);
        Assert.Single(profits);
        Assert.Equal(2025, profits[0].Year);
        Assert.Equal(1, profits[0].Month);
        Assert.Equal(0.5m, profits[0].TotalProfit);
    }
}
