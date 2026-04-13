using NUnit.Framework;
using Order.API.Tests.Helpers;
using Order.Model;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Order.API.Tests;

/// <summary>
/// Integration tests for GET /orders and GET /orders/{id}.
/// Verifies list retrieval, single-order detail, and 404 behaviour.
/// </summary>
[TestFixture]
public class GetOrderTests : ApiTestBase
{
    /// <summary>
    /// GET /orders returns 200 OK with all seeded orders.
    /// </summary>
    [Test]
    public async Task GetOrders_ReturnsAllOrders()
    {
        await _factory.AddOrder(_seed);
        await _factory.AddOrder(_seed);

        var response = await _client.GetAsync("/orders");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await DeserializeAsync<PagedResult<OrderSummary>>(response);
        Assert.That(result.Items.Count, Is.EqualTo(2));
    }

    /// <summary>
    /// GET /orders returns 200 OK with an empty list when no orders exist.
    /// </summary>
    [Test]
    public async Task GetOrders_ReturnsEmptyList_WhenNoOrders()
    {
        var response = await _client.GetAsync("/orders");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await DeserializeAsync<PagedResult<OrderSummary>>(response);
        Assert.That(result.Items, Is.Empty);
    }

    /// <summary>
    /// GET /orders/{id} returns 200 OK with correct order detail.
    /// </summary>
    [Test]
    public async Task GetOrderById_ReturnsOrderDetail()
    {
        var orderId = await _factory.AddOrder(_seed, quantity: 2);

        var response = await _client.GetAsync($"/orders/{orderId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var order = await DeserializeAsync<OrderDetail>(response);
        Assert.That(order.Id, Is.EqualTo(orderId));
        Assert.That(System.Linq.Enumerable.Count(order.Items), Is.EqualTo(1));
        Assert.That(order.TotalCost, Is.EqualTo(1.6m));
        Assert.That(order.TotalPrice, Is.EqualTo(1.8m));
    }

    /// <summary>
    /// GET /orders/{id} returns 404 Not Found for a non-existent order.
    /// </summary>
    [Test]
    public async Task GetOrderById_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync($"/orders/{Guid.NewGuid()}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
