using Microsoft.Extensions.DependencyInjection;
using OrderApi.Features.Orders;
using OrderApi.Features.Orders.GetOrders;
using OrderApi.Infrastructure;
using OrderApi.Tests.Common;
using System.Net;
using System.Net.Http.Json;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Integration tests for GET /orders — list, pagination, and input validation.
/// </summary>
[Collection("Orders")]
public sealed class GetOrdersTests : OrdersEndpointTestBase
{
    /// <summary>
    /// Initialises the test class with the shared application factory.
    /// </summary>
    /// <param name="factory">The <see cref="OrderApiTestFactory"/> fixture injected by xUnit.</param>
    public GetOrdersTests(OrderApiTestFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that GET /orders returns a list containing all seeded orders.
    /// </summary>
    [Fact]
    public async Task GetOrders_ReturnsAllOrders()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        var orderId = await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 2);

        var response = await Client.GetAsync("/api/v1/orders");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, o => o.Id == orderId);
    }

    /// <summary>
    /// Verifies that GET /orders returns an empty list with a total count of zero when no orders exist.
    /// </summary>
    [Fact]
    public async Task GetOrders_ReturnsEmptyList_WhenNoOrders()
    {
        var response = await Client.GetAsync("/api/v1/orders");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    /// <summary>
    /// Verifies that GET /orders returns 400 Bad Request when page=0 is requested.
    /// </summary>
    [Fact]
    public async Task GetOrders_ReturnsBadRequest_WhenPageZero()
    {
        var response = await Client.GetAsync("/api/v1/orders?page=0&pageSize=50");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that GET /orders returns 400 Bad Request when pageSize exceeds the maximum of 200.
    /// </summary>
    [Fact]
    public async Task GetOrders_ReturnsBadRequest_WhenPageSizeTooLarge()
    {
        var response = await Client.GetAsync("/api/v1/orders?page=1&pageSize=201");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that GET /orders returns 400 Bad Request when the page number exceeds the allowed limit.
    /// </summary>
    [Fact]
    public async Task GetOrders_ReturnsBadRequest_WhenPageExceedsLimit()
    {
        var response = await Client.GetAsync("/api/v1/orders?page=2000000&pageSize=50");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that GET /orders correctly pages results: page 1 of page-size 2 returns 2 items,
    /// page 2 returns the remaining 1 item, and total count is reported correctly.
    /// </summary>
    [Fact]
    public async Task GetOrders_SupportsPagination()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);

        for (var orderIndex = 0; orderIndex < 3; orderIndex++)
        {
            await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);
        }

        var page1 = await Client.GetFromJsonAsync<PagedResult<OrderSummaryResponse>>("/api/v1/orders?page=1&pageSize=2");
        Assert.NotNull(page1);
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page1.TotalPages);

        var page2 = await Client.GetFromJsonAsync<PagedResult<OrderSummaryResponse>>("/api/v1/orders?page=2&pageSize=2");
        Assert.NotNull(page2);
        Assert.Single(page2.Items);
    }

    /// <summary>
    /// Verifies that GET /orders returns 200 OK when pageSize is exactly at the maximum allowed value of 200.
    /// </summary>
    [Fact]
    public async Task GetOrders_ReturnsOk_WhenMaxPageSize()
    {
        var response = await Client.GetAsync("/api/v1/orders?pageSize=200");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Verifies that GET /orders returns 400 Bad Request when pageSize is one over the maximum allowed value.
    /// </summary>
    [Fact]
    public async Task GetOrders_Returns400_WhenPageSizeExceedsMax()
    {
        var response = await Client.GetAsync("/api/v1/orders?pageSize=201");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
