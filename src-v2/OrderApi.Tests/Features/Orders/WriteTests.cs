using Microsoft.Extensions.DependencyInjection;
using OrderApi.Features.Orders.CreateOrder;
using OrderApi.Features.Orders.GetOrderById;
using OrderApi.Features.Orders.UpdateOrderStatus;
using OrderApi.Infrastructure;
using OrderApi.Infrastructure.Entities;
using OrderApi.Tests.Common;
using System.Net;
using System.Net.Http.Json;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Integration tests for order write endpoints — POST /orders and PATCH /orders/{id}/status.
/// Covers 201/400 scenarios for order creation and 204/400/404 scenarios for status updates.
/// </summary>
[Collection("Orders")]
public sealed class WriteTests : OrdersEndpointTestBase
{
    /// <summary>
    /// Initialises the test class with the shared application factory.
    /// </summary>
    /// <param name="factory">The <see cref="OrderApiTestFactory"/> fixture injected by xUnit.</param>
    public WriteTests(OrderApiTestFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that POST /orders returns 201 Created when all required fields are valid.
    /// </summary>
    [Fact]
    public async Task CreateOrder_Returns201_WithValidRequest()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var request = new CreateOrderCommand(resellerId, customerId, [new CreateOrderItemRequest(productId, 3)]);

        var response = await Client.PostAsJsonAsync("/api/v1/orders", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Verifies that POST /orders returns 201 Created when the order contains multiple line items.
    /// </summary>
    [Fact]
    public async Task CreateOrder_Returns201_WithMultipleItems()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId1, serviceId) = await SeedReferenceDataAsync(db);

        var productId2 = Guid.NewGuid();
        db.OrderProducts.Add(new OrderProduct
        {
            Id = productId2.ToByteArray(),
            ServiceId = serviceId.ToByteArray(),
            Name = "Basic Antivirus",
            UnitCost = 1.5m,
            UnitPrice = 2.0m
        });
        await db.SaveChangesAsync();

        var request = new CreateOrderCommand(resellerId, customerId,
            [new CreateOrderItemRequest(productId1, 2), new CreateOrderItemRequest(productId2, 3)]);

        var response = await Client.PostAsJsonAsync("/api/v1/orders", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Verifies that POST /orders returns 400 Bad Request when the items list is empty.
    /// </summary>
    [Fact]
    public async Task CreateOrder_Returns400_WhenItemsEmpty()
    {
        var request = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<CreateOrderItemRequest>());
        var response = await Client.PostAsJsonAsync("/api/v1/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that POST /orders returns 400 Bad Request when the product ID does not exist in the database.
    /// </summary>
    [Fact]
    public async Task CreateOrder_Returns400_WhenProductIdInvalid()
    {
        var request = new CreateOrderCommand(Guid.NewGuid(), Guid.NewGuid(), [new CreateOrderItemRequest(Guid.NewGuid(), 1)]);
        var response = await Client.PostAsJsonAsync("/api/v1/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that a created order can be retrieved from GET /orders/{id} with the correct data.
    /// </summary>
    [Fact]
    public async Task CreateOrder_PersistsAndIsRetrievable()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var request = new CreateOrderCommand(resellerId, customerId, [new CreateOrderItemRequest(productId, 5)]);

        var createResponse = await Client.PostAsJsonAsync("/api/v1/orders", request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var location = createResponse.Headers.Location?.ToString();
        Assert.NotNull(location);

        var getResponse = await Client.GetAsync(location);
        getResponse.EnsureSuccessStatusCode();

        var order = await getResponse.Content.ReadFromJsonAsync<OrderDetailResponse>();
        Assert.NotNull(order);
        Assert.Single(order.Items);
        Assert.Equal(5, order.Items[0].Quantity);
        Assert.Equal("Created", order.StatusName);
    }

    /// <summary>
    /// Verifies that POST /orders returns 400 Bad Request when the request body is null.
    /// </summary>
    [Fact]
    public async Task CreateOrder_Returns400_WhenBodyIsNull()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/orders", (CreateOrderCommand?)null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that POST /orders returns 400 Bad Request when the same product ID appears more than once.
    /// </summary>
    [Fact]
    public async Task CreateOrder_Returns400_WhenDuplicateProductIds()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var request = new CreateOrderCommand(resellerId, customerId,
            [new CreateOrderItemRequest(productId, 1), new CreateOrderItemRequest(productId, 2)]);

        var response = await Client.PostAsJsonAsync("/api/v1/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that POST /orders returns 400 Bad Request when the items list exceeds the maximum of 100 items.
    /// </summary>
    [Fact]
    public async Task CreateOrder_Returns400_WhenTooManyItems()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, _, _) = await SeedReferenceDataAsync(db);

        var items = Enumerable.Range(0, 101)
            .Select(_ => new CreateOrderItemRequest(Guid.NewGuid(), 1))
            .ToList();

        var request = new CreateOrderCommand(resellerId, customerId, items);

        var response = await Client.PostAsJsonAsync("/api/v1/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that POST /orders returns 201 Created when the quantity is exactly at the maximum allowed value of 1,000,000.
    /// </summary>
    [Fact]
    public async Task CreateOrder_Returns201_WhenMaxQuantity()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var request = new CreateOrderCommand(resellerId, customerId, [new CreateOrderItemRequest(productId, 1_000_000)]);

        var response = await Client.PostAsJsonAsync("/api/v1/orders", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    /// <summary>
    /// Verifies that POST /orders returns 400 Bad Request when the quantity exceeds the maximum allowed value.
    /// </summary>
    [Fact]
    public async Task CreateOrder_Returns400_WhenQuantityExceedsMax()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var request = new CreateOrderCommand(resellerId, customerId, [new CreateOrderItemRequest(productId, 1_000_001)]);

        var response = await Client.PostAsJsonAsync("/api/v1/orders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that PATCH /orders/{id}/status returns 204 No Content when the status name is valid.
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatus_Returns204_WhenValid()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        var orderId = await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        var response = await Client.PatchAsJsonAsync($"/api/v1/orders/{orderId}/status", new { StatusName = "Completed" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    /// Verifies that the order's status is actually persisted after a successful PATCH /orders/{id}/status call.
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatus_ActuallyChangesStatus()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        var orderId = await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        await Client.PatchAsJsonAsync($"/api/v1/orders/{orderId}/status", new { StatusName = "Completed" });

        var response = await Client.GetAsync($"/api/v1/orders/{orderId}");
        var order = await response.Content.ReadFromJsonAsync<OrderDetailResponse>();
        Assert.NotNull(order);
        Assert.Equal("Completed", order.StatusName);
    }

    /// <summary>
    /// Verifies that PATCH /orders/{id}/status returns 404 Not Found when the order does not exist.
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatus_Returns404_WhenOrderNotFound()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        await SeedReferenceDataAsync(db);

        var response = await Client.PatchAsJsonAsync($"/api/v1/orders/{Guid.NewGuid()}/status", new { StatusName = "Completed" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that PATCH /orders/{id}/status is case-insensitive and accepts lowercase status names.
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatus_Returns204_WhenStatusNameIsLowercase()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        var orderId = await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        var response = await Client.PatchAsJsonAsync($"/api/v1/orders/{orderId}/status", new { StatusName = "completed" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    /// Verifies that PATCH /orders/{id}/status returns 400 Bad Request when the status name is not a recognised lifecycle value.
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatus_Returns400_WhenStatusInvalid()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        var orderId = await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        var response = await Client.PatchAsJsonAsync($"/api/v1/orders/{orderId}/status", new { StatusName = "NotAStatus" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that PATCH /orders/{id}/status returns 400 Bad Request when the request body is null.
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatus_Returns400_WhenBodyIsNull()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (_, _, statusId, productId, serviceId) = await SeedReferenceDataAsync(db);
        var orderId = await SeedOrderAsync(db, statusId, productId, serviceId, quantity: 1);

        var response = await Client.PatchAsJsonAsync($"/api/v1/orders/{orderId}/status", (UpdateOrderStatusRequest?)null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
