using Microsoft.Extensions.DependencyInjection;
using OrderApi.Features.Orders;
using OrderApi.Features.Orders.CreateOrder;
using OrderApi.Features.Orders.GetOrderHistory;
using OrderApi.Features.Orders.GetOrders;
using OrderApi.Features.Orders.UpdateOrderStatus;
using OrderApi.Infrastructure;
using OrderApi.Tests.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Integration tests for soft-delete, order history, and order search endpoints.
/// Covers DELETE /orders/{id}, GET /orders/deleted, GET /orders/{id}/history, and GET /orders/search.
/// </summary>
[Collection("Orders")]
public sealed class SoftDeleteHistorySearchTests : OrdersEndpointTestBase
{
    /// <summary>
    /// Initialises the test class with the shared application factory.
    /// </summary>
    /// <param name="factory">The <see cref="OrderApiTestFactory"/> fixture injected by xUnit.</param>
    public SoftDeleteHistorySearchTests(OrderApiTestFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that DELETE /orders/{id} returns 204 and the order is no longer returned by GET /orders/{id}.
    /// </summary>
    [Fact]
    public async Task DeleteOrder_Returns204_AndHidesFromGetOrders()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        var orderId = await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        var deleteResponse = await Client.DeleteAsync($"/api/v1/orders/{orderId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await Client.GetAsync($"/api/v1/orders/{orderId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    /// <summary>
    /// Verifies that DELETE /orders/{id} returns 404 Not Found when the order does not exist.
    /// </summary>
    [Fact]
    public async Task DeleteOrder_Returns404_WhenNotFound()
    {
        var response = await Client.DeleteAsync($"/api/v1/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that GET /orders/deleted returns the order after it has been soft-deleted.
    /// </summary>
    [Fact]
    public async Task GetDeletedOrders_ShowsSoftDeletedOrders()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        var orderId = await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        await Client.DeleteAsync($"/api/v1/orders/{orderId}");

        var response = await Client.GetAsync("/api/v1/orders/deleted");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, o => o.Id == orderId);
    }

    /// <summary>
    /// Verifies that GET /orders/deleted returns an empty list when no orders have been soft-deleted.
    /// </summary>
    [Fact]
    public async Task GetDeletedOrders_ReturnsEmpty_WhenNoDeleted()
    {
        var response = await Client.GetAsync("/api/v1/orders/deleted");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    /// <summary>
    /// Verifies that GET /orders/{id}/history returns a single transition entry after a status update.
    /// </summary>
    [Fact]
    public async Task GetOrderHistory_ReturnsTransitions_AfterStatusUpdate()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var createResponse = await Client.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(resellerId, customerId, [new(productId, 1)]));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.GetProperty("id").GetGuid();

        await Client.PatchAsJsonAsync($"/api/v1/orders/{orderId}/status", new UpdateOrderStatusRequest("In Progress"));

        var historyResponse = await Client.GetAsync($"/api/v1/orders/{orderId}/history");
        historyResponse.EnsureSuccessStatusCode();

        var result = await historyResponse.Content.ReadFromJsonAsync<PagedResult<OrderStatusHistoryResponse>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Created", result.Items[0].FromStatus);
        Assert.Equal("In Progress", result.Items[0].ToStatus);
    }

    /// <summary>
    /// Verifies that GET /orders/{id}/history returns an empty list for a newly created order that has not changed status.
    /// </summary>
    [Fact]
    public async Task GetOrderHistory_ReturnsEmpty_WhenNoTransitions()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var createResponse = await Client.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(resellerId, customerId, [new(productId, 1)]));
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.GetProperty("id").GetGuid();

        var historyResponse = await Client.GetAsync($"/api/v1/orders/{orderId}/history");
        historyResponse.EnsureSuccessStatusCode();

        var result = await historyResponse.Content.ReadFromJsonAsync<PagedResult<OrderStatusHistoryResponse>>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    /// <summary>
    /// Verifies that GET /orders/search?status=Created returns only orders with the Created status.
    /// </summary>
    [Fact]
    public async Task SearchOrders_FiltersByStatus()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var createResponse = await Client.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(resellerId, customerId, [new(productId, 1)]));
        createResponse.EnsureSuccessStatusCode();

        var response = await Client.GetAsync("/api/v1/orders/search?status=Created");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, o => Assert.Equal("Created", o.StatusName));
    }

    /// <summary>
    /// Verifies that GET /orders/search without any filters returns all active orders.
    /// </summary>
    [Fact]
    public async Task SearchOrders_NoFilters_ReturnsAll()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        await Client.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(resellerId, customerId, [new(productId, 2)]));

        var response = await Client.GetAsync("/api/v1/orders/search");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    /// <summary>
    /// Verifies that GET /orders/search with a date range filter returns only orders within the specified window.
    /// </summary>
    [Fact]
    public async Task SearchOrders_FiltersByDateRange()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        await Client.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(resellerId, customerId, [new(productId, 1)]));

        var from = DateTime.UtcNow.AddDays(-1).ToString("O");
        var to = DateTime.UtcNow.AddDays(1).ToString("O");

        var response = await Client.GetAsync($"/api/v1/orders/search?from={from}&to={to}");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
    }

    /// <summary>
    /// Verifies that GET /orders/search returns an empty result when no orders match the filter.
    /// </summary>
    [Fact]
    public async Task SearchOrders_NoMatch_ReturnsEmpty()
    {
        var response = await Client.GetAsync("/api/v1/orders/search?status=Completed");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    /// <summary>
    /// Verifies that GET /orders/search?minTotal filters out orders whose total price is below the threshold.
    /// </summary>
    [Fact]
    public async Task SearchOrders_FiltersByMinTotal()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        await Client.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(resellerId, customerId, [new(productId, 1)]));
        await Client.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(resellerId, customerId, [new(productId, 10)]));

        var response = await Client.GetAsync("/api/v1/orders/search?minTotal=5");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.True(result.Items[0].TotalPrice >= 5m);
    }
}
