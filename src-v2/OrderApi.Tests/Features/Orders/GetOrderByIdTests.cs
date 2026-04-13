using Microsoft.Extensions.DependencyInjection;
using OrderApi.Features.Orders.GetOrderById;
using OrderApi.Infrastructure;
using OrderApi.Tests.Common;
using System.Net;
using System.Net.Http.Json;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Integration tests for GET /orders/{id} — single order retrieval and computed totals.
/// </summary>
[Collection("Orders")]
public sealed class GetOrderByIdTests : OrdersEndpointTestBase
{
    /// <summary>
    /// Initialises the test class with the shared application factory.
    /// </summary>
    /// <param name="factory">The <see cref="OrderApiTestFactory"/> fixture injected by xUnit.</param>
    public GetOrderByIdTests(OrderApiTestFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that GET /orders/{id} returns the correct order when it exists.
    /// </summary>
    [Fact]
    public async Task GetOrderById_ReturnsOrder_WhenExists()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        var orderId = await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        var response = await Client.GetAsync($"/api/v1/orders/{orderId}");
        response.EnsureSuccessStatusCode();

        var order = await response.Content.ReadFromJsonAsync<OrderDetailResponse>();
        Assert.NotNull(order);
        Assert.Equal(orderId, order.Id);
    }

    /// <summary>
    /// Verifies that GET /orders/{id} returns correct TotalCost and TotalPrice computed from quantity and unit values.
    /// </summary>
    [Fact]
    public async Task GetOrderById_ReturnsItemsWithCorrectTotals()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        var orderId = await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 3);

        var response = await Client.GetAsync($"/api/v1/orders/{orderId}");
        response.EnsureSuccessStatusCode();

        var order = await response.Content.ReadFromJsonAsync<OrderDetailResponse>();
        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(2.4m, order.TotalCost);
        Assert.Equal(2.7m, order.TotalPrice);
    }

    /// <summary>
    /// Verifies that GET /orders/{id} returns 404 Not Found when the order does not exist.
    /// </summary>
    [Fact]
    public async Task GetOrderById_Returns404_WhenNotFound()
    {
        var response = await Client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
