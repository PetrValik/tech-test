using NUnit.Framework;
using Order.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Service.Tests;

/// <summary>
/// Unit/integration tests for <see cref="IOrderService.CreateOrderAsync"/> using a real SQLite
/// in-memory database to exercise the repository and service layers together.
/// </summary>
[TestFixture]
public class CreateOrderServiceTests : ServiceTestBase
{
    [Test]
    public async Task CreateOrderAsync_CreatesOrderAndReturnsId()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new[]
            {
                new CreateOrderItemRequest { ProductId = new Guid(_orderProductEmailId), Quantity = 1 }
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.OrderId, Is.Not.Null);
        Assert.That(result.OrderId, Is.Not.EqualTo(Guid.Empty));
        var order = await _orderService.GetOrderByIdAsync(result.OrderId!.Value);
        Assert.That(order, Is.Not.Null);
    }

    [Test]
    public async Task CreateOrderAsync_CreatesOrderWithItems()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new[]
            {
                new CreateOrderItemRequest { ProductId = new Guid(_orderProductEmailId), Quantity = 3 }
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(request);
        var order = await _orderService.GetOrderByIdAsync(result.OrderId!.Value);

        // Assert
        Assert.That(order!.Items.Count(), Is.EqualTo(1));
        Assert.That(order.Items.First().Quantity, Is.EqualTo(3));
    }

    [Test]
    public async Task CreateOrderAsync_ReturnsFailure_WhenProductIdIsInvalid()
    {
        // Arrange
        var invalidProductId = Guid.NewGuid();
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new[]
            {
                new CreateOrderItemRequest { ProductId = invalidProductId, Quantity = 1 }
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(request);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.OrderId, Is.Null);
        Assert.That(result.InvalidProductIds.Count, Is.EqualTo(1));
        Assert.That(result.InvalidProductIds[0], Is.EqualTo(invalidProductId));
    }

    [Test]
    public async Task CreateOrderAsync_CreatesOrderWithMultipleItems()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            ResellerId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            Items = new[]
            {
                new CreateOrderItemRequest { ProductId = new Guid(_orderProductEmailId), Quantity = 2 },
                new CreateOrderItemRequest { ProductId = new Guid(_orderProductAntivirusId), Quantity = 5 }
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(request);
        var order = await _orderService.GetOrderByIdAsync(result.OrderId!.Value);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(order!.Items.Count(), Is.EqualTo(2));
    }
}
