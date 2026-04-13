using Microsoft.Extensions.DependencyInjection;
using OrderApi.Features.Orders;
using OrderApi.Features.Orders.CreateOrder;
using OrderApi.Features.Orders.GetOrders;
using OrderApi.Features.Orders.UpdateOrderStatus;
using OrderApi.Infrastructure;
using OrderApi.Tests.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Integration tests for ETag-based optimistic concurrency and idempotency key deduplication.
/// Covers GET /orders/{id} ETag headers, PATCH If-Match validation, and POST Idempotency-Key replay.
/// </summary>
[Collection("Orders")]
public sealed class ConcurrencyIdempotencyTests : OrdersEndpointTestBase
{
    /// <summary>
    /// Initialises the test class with the shared application factory.
    /// </summary>
    /// <param name="factory">The <see cref="OrderApiTestFactory"/> fixture injected by xUnit.</param>
    public ConcurrencyIdempotencyTests(OrderApiTestFactory factory) : base(factory) { }

    /// <summary>
    /// Verifies that GET /orders/{id} includes an ETag response header containing the concurrency stamp.
    /// </summary>
    [Fact]
    public async Task GetOrderById_ReturnsETagHeader()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var createResponse = await Client.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(resellerId, customerId, [new(productId, 1)]));
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.GetProperty("id").GetGuid();

        var getResponse = await Client.GetAsync($"/api/v1/orders/{orderId}");
        getResponse.EnsureSuccessStatusCode();

        Assert.True(getResponse.Headers.ETag is not null, "Response should have ETag header");
    }

    /// <summary>
    /// Verifies that PATCH /orders/{id}/status returns 204 No Content when the If-Match header matches the current ETag.
    /// </summary>
    [Fact]
    public async Task UpdateStatus_WithCorrectIfMatch_Returns204()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var createResponse = await Client.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(resellerId, customerId, [new(productId, 1)]));
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.GetProperty("id").GetGuid();

        var getResponse = await Client.GetAsync($"/api/v1/orders/{orderId}");
        var etag = getResponse.Headers.ETag?.Tag;
        Assert.NotNull(etag);

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/orders/{orderId}/status");
        patchRequest.Headers.Add("If-Match", etag);
        patchRequest.Content = JsonContent.Create(new UpdateOrderStatusRequest("In Progress"));

        var patchResponse = await Client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.NoContent, patchResponse.StatusCode);
    }

    /// <summary>
    /// Verifies that PATCH /orders/{id}/status returns 409 Conflict when the If-Match header contains a stale ETag.
    /// </summary>
    [Fact]
    public async Task UpdateStatus_WithStaleIfMatch_Returns409Conflict()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var createResponse = await Client.PostAsJsonAsync("/api/v1/orders", new CreateOrderCommand(resellerId, customerId, [new(productId, 1)]));
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.GetProperty("id").GetGuid();

        var patchRequest = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/orders/{orderId}/status");
        patchRequest.Headers.Add("If-Match", "\"stale-etag-value\"");
        patchRequest.Content = JsonContent.Create(new UpdateOrderStatusRequest("In Progress"));

        var patchResponse = await Client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.Conflict, patchResponse.StatusCode);
    }

    /// <summary>
    /// Verifies that two sequential POST /orders requests with the same Idempotency-Key
    /// produce identical responses and only one order is stored in the database.
    /// </summary>
    [Fact]
    public async Task PostWithIdempotencyKey_DeduplicatesRequests()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var idempotencyKey = Guid.NewGuid().ToString();
        var command = new CreateOrderCommand(resellerId, customerId, [new(productId, 1)]);

        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders");
        request1.Headers.Add("Idempotency-Key", idempotencyKey);
        request1.Content = JsonContent.Create(command);
        var response1 = await Client.SendAsync(request1);
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        var body1 = await response1.Content.ReadAsStringAsync();

        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders");
        request2.Headers.Add("Idempotency-Key", idempotencyKey);
        request2.Content = JsonContent.Create(command);
        var response2 = await Client.SendAsync(request2);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);
        var body2 = await response2.Content.ReadAsStringAsync();

        Assert.Equal(body1, body2);

        var ordersResponse = await Client.GetAsync("/api/v1/orders");
        var orders = await ordersResponse.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(orders);
        Assert.Equal(1, orders.TotalCount);
    }

    /// <summary>
    /// Verifies that two concurrent POST /orders requests with the same Idempotency-Key
    /// both return 201 Created with identical bodies, and only one order is persisted.
    /// </summary>
    [Fact]
    public async Task PostWithIdempotencyKey_DeduplicatesParallelRequests()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var idempotencyKey = Guid.NewGuid().ToString();
        var command = new CreateOrderCommand(resellerId, customerId, [new(productId, 1)]);

        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders");
        request1.Headers.Add("Idempotency-Key", idempotencyKey);
        request1.Content = JsonContent.Create(command);

        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders");
        request2.Headers.Add("Idempotency-Key", idempotencyKey);
        request2.Content = JsonContent.Create(command);

        var responses = await Task.WhenAll(Client.SendAsync(request1), Client.SendAsync(request2));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));

        var body1 = await responses[0].Content.ReadAsStringAsync();
        var body2 = await responses[1].Content.ReadAsStringAsync();
        Assert.Equal(body1, body2);

        var ordersResponse = await Client.GetAsync("/api/v1/orders");
        var orders = await ordersResponse.Content.ReadFromJsonAsync<PagedResult<OrderSummaryResponse>>();
        Assert.NotNull(orders);
        Assert.Equal(1, orders.TotalCount);
    }

    /// <summary>
    /// Verifies that POST /orders without an Idempotency-Key header still creates the order normally.
    /// </summary>
    [Fact]
    public async Task PostWithoutIdempotencyKey_StillCreatesOrder()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var (resellerId, customerId, _, productId, _) = await SeedReferenceDataAsync(db);

        var command = new CreateOrderCommand(resellerId, customerId, [new(productId, 1)]);
        var response = await Client.PostAsJsonAsync("/api/v1/orders", command);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
