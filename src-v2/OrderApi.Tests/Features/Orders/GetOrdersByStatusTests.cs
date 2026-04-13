using Microsoft.Extensions.DependencyInjection;
using OrderApi.Features.Orders;
using OrderApi.Features.Orders.GetOrders;
using OrderApi.Infrastructure;
using OrderApi.Tests.Common;
using System.Net;
using System.Net.Http.Json;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Integration tests for GET /orders/status/{name} — status-based filtering, pagination, and case sensitivity.
/// </summary>
[Collection("Orders")]
public sealed class GetOrdersByStatusTests : OrdersEndpointTestBase
{
    /// <summary>
    /// Initialises the test class with the shared application factory.
    /// </summary>
    /// <param name="factory">The <see cref="OrderApiTestFactory"/> fixture injected by xUnit.</param>
    public GetOrdersByStatusTests(OrderApiTestFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that GET /orders/status/{name} returns only orders that match the given status.
    /// </summary>
    [Fact]
    public async Task GetOrdersByStatus_ReturnsFilteredOrders()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        var response = await Client.GetAsync("/api/v1/orders/status/Created");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.All(result.Items, o => Assert.Equal("Created", o.StatusName));
    }

    /// <summary>
    /// Verifies that GET /orders/status/{name} returns an empty list when no orders have the requested status.
    /// </summary>
    [Fact]
    public async Task GetOrdersByStatus_ReturnsEmpty_WhenNoMatchingOrders()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        var response = await Client.GetAsync("/api/v1/orders/status/Failed");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    /// <summary>
    /// Verifies that GET /orders/status/{name} returns 400 Bad Request when page=0 is requested.
    /// </summary>
    [Fact]
    public async Task GetOrdersByStatus_ReturnsBadRequest_WhenPageZero()
    {
        var response = await Client.GetAsync("/api/v1/orders/status/Created?page=0&pageSize=50");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that GET /orders/status/{name} returns an empty result for an unrecognised status name
    /// rather than returning 404 or throwing.
    /// </summary>
    [Fact]
    public async Task GetOrdersByStatus_ReturnsEmpty_WhenStatusUnknown()
    {
        var response = await Client.GetAsync("/api/v1/orders/status/Nonexistent");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    /// <summary>
    /// Verifies that GET /orders/status/{name} is case-insensitive — passing a lowercase name returns the same results.
    /// </summary>
    [Fact]
    public async Task GetOrdersByStatus_IsCaseInsensitive()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        var response = await Client.GetAsync("/api/v1/orders/status/created");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, o => Assert.Equal("Created", o.StatusName));
    }
}
