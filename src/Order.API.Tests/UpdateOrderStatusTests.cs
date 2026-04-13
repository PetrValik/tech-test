using NUnit.Framework;
using Order.API.Tests.Helpers;
using Order.Model;
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Order.API.Tests;

/// <summary>
/// Integration tests for PATCH /orders/{id}/status.
/// Covers success, null-body guard, unknown order, empty status name, unrecognised status name.
/// </summary>
[TestFixture]
public class UpdateOrderStatusTests : ApiTestBase
{
    /// <summary>
    /// PATCH /orders/{id}/status returns 204 No Content on success and persists the change.
    /// </summary>
    [Test]
    public async Task UpdateOrderStatus_Returns204_OnSuccess()
    {
        var orderId = await _factory.AddOrder(_seed);

        var request  = new UpdateOrderStatusRequest { StatusName = "Completed" };
        var response = await _client.PatchAsJsonAsync($"/orders/{orderId}/status", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify the status change persisted
        var detail = await DeserializeAsync<OrderDetail>(
            await _client.GetAsync($"/orders/{orderId}"));
        Assert.That(detail.StatusName, Is.EqualTo("Completed"));
    }

    /// <summary>
    /// PATCH /orders/{id}/status returns 404 when the order does not exist.
    /// </summary>
    [Test]
    public async Task UpdateOrderStatus_Returns404_WhenOrderNotFound()
    {
        var request  = new UpdateOrderStatusRequest { StatusName = "Completed" };
        var response = await _client.PatchAsJsonAsync($"/orders/{Guid.NewGuid()}/status", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// PATCH /orders/{id}/status returns 400 when StatusName is empty.
    /// </summary>
    [Test]
    public async Task UpdateOrderStatus_Returns400_WhenStatusNameEmpty()
    {
        var orderId  = await _factory.AddOrder(_seed);
        var request  = new UpdateOrderStatusRequest { StatusName = "" };
        var response = await _client.PatchAsJsonAsync($"/orders/{orderId}/status", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// PATCH /orders/{id}/status returns 400 when StatusName is not a recognised value.
    /// </summary>
    [Test]
    public async Task UpdateOrderStatus_Returns400_WhenStatusNameUnknown()
    {
        var orderId  = await _factory.AddOrder(_seed);
        var request  = new UpdateOrderStatusRequest { StatusName = "NonExistentStatus" };
        var response = await _client.PatchAsJsonAsync($"/orders/{orderId}/status", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// PATCH /orders/{id}/status with a JSON null body returns 400 Bad Request (not 500).
    /// </summary>
    [Test]
    public async Task UpdateOrderStatus_Returns400_WhenBodyIsNull()
    {
        var orderId  = await _factory.AddOrder(_seed);
        var response = await _client.PatchAsJsonAsync<UpdateOrderStatusRequest?>($"/orders/{orderId}/status", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
