using NUnit.Framework;
using Order.API.Tests.Helpers;
using Order.Model;
using System.Net;
using System.Threading.Tasks;

namespace Order.API.Tests;

/// <summary>
/// Integration tests for GET /orders/status/{statusName}.
/// Verifies filtered list retrieval and empty-result handling.
/// </summary>
[TestFixture]
public class GetOrdersByStatusTests : ApiTestBase
{
    /// <summary>
    /// GET /orders/status/Created returns only orders with that status.
    /// </summary>
    [Test]
    public async Task GetOrdersByStatus_ReturnsFilteredOrders()
    {
        await _factory.AddOrder(_seed, statusId: _seed.StatusCreatedId);
        await _factory.AddOrder(_seed, statusId: _seed.StatusCompletedId);

        var response = await _client.GetAsync("/orders/status/Created");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await DeserializeAsync<PagedResult<OrderSummary>>(response);
        Assert.That(result.Items.Count, Is.EqualTo(1));
        Assert.That(result.Items[0].StatusName, Is.EqualTo("Created"));
    }

    /// <summary>
    /// GET /orders/status/{statusName} returns 200 OK with an empty list when no match.
    /// </summary>
    [Test]
    public async Task GetOrdersByStatus_ReturnsEmpty_WhenNoMatch()
    {
        await _factory.AddOrder(_seed, statusId: _seed.StatusCreatedId);

        var response = await _client.GetAsync("/orders/status/NonExistent");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await DeserializeAsync<PagedResult<OrderSummary>>(response);
        Assert.That(result.Items, Is.Empty);
    }
}
