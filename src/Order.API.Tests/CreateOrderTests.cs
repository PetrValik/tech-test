using NUnit.Framework;
using Order.API.Tests.Helpers;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Order.API.Tests;

/// <summary>
/// Integration tests for POST /orders.
/// Covers success cases, validation failures, duplicate product IDs, quantity limits, and roundtrip verification.
/// </summary>
[TestFixture]
public class CreateOrderTests : ApiTestBase
{
    /// <summary>
    /// POST /orders returns 201 Created with a Location header pointing to the new order.
    /// </summary>
    [Test]
    public async Task CreateOrder_Returns201_WithLocation()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest
                {
                    ProductId = new Guid(_seed.ProductEmailId),
                    Quantity  = 1
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/orders", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response.Headers.Location, Is.Not.Null);
    }

    /// <summary>
    /// POST /orders with empty ResellerId returns 400 Bad Request.
    /// </summary>
    [Test]
    public async Task CreateOrder_Returns400_WhenResellerIdEmpty()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.Empty,
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = new Guid(_seed.ProductEmailId), Quantity = 1 }
            }
        };

        var response = await _client.PostAsJsonAsync("/orders", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// POST /orders with an empty items list returns 400 Bad Request.
    /// </summary>
    [Test]
    public async Task CreateOrder_Returns400_WhenItemsEmpty()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>()
        };

        var response = await _client.PostAsJsonAsync("/orders", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// POST /orders with Quantity = 0 returns 400 Bad Request.
    /// </summary>
    [Test]
    public async Task CreateOrder_Returns400_WhenQuantityZero()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = new Guid(_seed.ProductEmailId), Quantity = 0 }
            }
        };

        var response = await _client.PostAsJsonAsync("/orders", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// POST /orders with an unknown product ID returns 400 Bad Request.
    /// </summary>
    [Test]
    public async Task CreateOrder_Returns400_WhenProductIdUnknown()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 }
            }
        };

        var response = await _client.PostAsJsonAsync("/orders", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// POST /orders with a JSON null body returns 400 Bad Request (not 500).
    /// </summary>
    [Test]
    public async Task CreateOrder_Returns400_WhenBodyIsNull()
    {
        var response = await _client.PostAsJsonAsync<CreateOrderRequest?>("/orders", null);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// POST /orders with duplicate ProductIds returns 400 Bad Request.
    /// </summary>
    [Test]
    public async Task CreateOrder_Returns400_WhenDuplicateProductIds()
    {
        var productId = new Guid(_seed.ProductEmailId);
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest { ProductId = productId, Quantity = 1 },
                new CreateOrderItemRequest { ProductId = productId, Quantity = 2 }
            }
        };

        var response = await _client.PostAsJsonAsync("/orders", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// POST /orders with more than 100 items returns 400 Bad Request.
    /// </summary>
    [Test]
    public async Task CreateOrder_Returns400_WhenTooManyItems()
    {
        var items = new List<CreateOrderItemRequest>();
        for (int index = 0; index < 101; index++)
        {
            items.Add(new CreateOrderItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 });
        }

        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = items
        };

        var response = await _client.PostAsJsonAsync("/orders", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// POST /orders followed by GET /orders/{id} returns the same order (roundtrip).
    /// </summary>
    [Test]
    public async Task CreateOrder_RoundTrip_ReturnsCreatedOrder()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest
                {
                    ProductId = new Guid(_seed.ProductEmailId),
                    Quantity  = 5
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/orders", request);
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var location = createResponse.Headers.Location!.ToString();
        var getResponse = await _client.GetAsync(location);
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var order = await DeserializeAsync<OrderDetail>(getResponse);
        Assert.That(order.ResellerId, Is.EqualTo(request.ResellerId));
        Assert.That(order.CustomerId, Is.EqualTo(request.CustomerId));
        Assert.That(order.StatusName, Is.EqualTo("Created"));
    }

    /// <summary>
    /// POST /orders with maximum allowed Quantity (1,000,000) succeeds.
    /// </summary>
    [Test]
    public async Task CreateOrder_Returns201_WhenMaxQuantity()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest
                {
                    ProductId = new Guid(_seed.ProductEmailId),
                    Quantity  = 1_000_000
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/orders", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    /// <summary>
    /// POST /orders with Quantity exceeding 1,000,000 returns 400 Bad Request.
    /// </summary>
    [Test]
    public async Task CreateOrder_Returns400_WhenQuantityExceedsMax()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest
                {
                    ProductId = new Guid(_seed.ProductEmailId),
                    Quantity  = 1_000_001
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/orders", request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    /// <summary>
    /// POST /orders with multiple different products returns 201 and all items persisted.
    /// </summary>
    [Test]
    public async Task CreateOrder_Returns201_WithMultipleProducts()
    {
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderItemRequest>
            {
                new CreateOrderItemRequest
                {
                    ProductId = new Guid(_seed.ProductEmailId),
                    Quantity  = 3
                },
                new CreateOrderItemRequest
                {
                    ProductId = new Guid(_seed.ProductAntivirusId),
                    Quantity  = 2
                }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/orders", request);
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var location = createResponse.Headers.Location!.ToString();
        var getResponse = await _client.GetAsync(location);
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var detail = await DeserializeAsync<OrderDetail>(getResponse);
        Assert.That(detail.Items.Count, Is.EqualTo(2));
    }
}
