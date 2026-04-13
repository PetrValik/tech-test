using NUnit.Framework;
using Order.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Service.Tests;

/// <summary>
/// Unit/integration tests for <see cref="IOrderService.GetOrdersAsync"/>,
/// <see cref="IOrderService.GetOrderByIdAsync"/>, and
/// <see cref="IOrderService.GetOrdersByStatusAsync"/> using a real SQLite in-memory database.
/// </summary>
[TestFixture]
public class GetOrderServiceTests : ServiceTestBase
{
    [Test]
    public async Task GetOrdersAsync_ReturnsCorrectNumberOfOrders()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        await AddOrder(orderId1, 1);

        var orderId2 = Guid.NewGuid();
        await AddOrder(orderId2, 2);

        var orderId3 = Guid.NewGuid();
        await AddOrder(orderId3, 3);

        // Act
        var result = await _orderService.GetOrdersAsync();

        // Assert
        Assert.That(result.Items.Count, Is.EqualTo(3));
    }

    [Test]
    public async Task GetOrdersAsync_ReturnsOrdersWithCorrectTotals()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        await AddOrder(orderId1, 1);

        var orderId2 = Guid.NewGuid();
        await AddOrder(orderId2, 2);

        var orderId3 = Guid.NewGuid();
        await AddOrder(orderId3, 3);

        // Act
        var result = await _orderService.GetOrdersAsync();

        // Assert
        var order1 = result.Items.SingleOrDefault(order => order.Id == orderId1);
        var order2 = result.Items.SingleOrDefault(order => order.Id == orderId2);
        var order3 = result.Items.SingleOrDefault(order => order.Id == orderId3);

        Assert.That(order1!.TotalCost, Is.EqualTo(0.8m));
        Assert.That(order1.TotalPrice, Is.EqualTo(0.9m));

        Assert.That(order2!.TotalCost, Is.EqualTo(1.6m));
        Assert.That(order2.TotalPrice, Is.EqualTo(1.8m));

        Assert.That(order3!.TotalCost, Is.EqualTo(2.4m));
        Assert.That(order3.TotalPrice, Is.EqualTo(2.7m));
    }

    [Test]
    public async Task GetOrderByIdAsync_ReturnsNull_WhenOrderDoesNotExist()
    {
        // Act
        var order = await _orderService.GetOrderByIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(order, Is.Null);
    }

    [Test]
    public async Task GetOrderByIdAsync_ReturnsCorrectOrder()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        await AddOrder(orderId1, 1);

        // Act
        var order = await _orderService.GetOrderByIdAsync(orderId1);

        // Assert
        Assert.That(order!.Id, Is.EqualTo(orderId1));
    }

    [Test]
    public async Task GetOrderByIdAsync_ReturnsCorrectOrderItemCount()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        await AddOrder(orderId1, 1);

        // Act
        var order = await _orderService.GetOrderByIdAsync(orderId1);

        // Assert
        Assert.That(order!.Items.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetOrderByIdAsync_ReturnsOrderWithCorrectTotals()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        await AddOrder(orderId1, 2);

        // Act
        var order = await _orderService.GetOrderByIdAsync(orderId1);

        // Assert
        Assert.That(order!.TotalCost, Is.EqualTo(1.6m));
        Assert.That(order.TotalPrice, Is.EqualTo(1.8m));
    }

    [Test]
    public async Task GetOrdersByStatusAsync_ReturnsOnlyOrdersWithMatchingStatus()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        await AddOrder(orderId1, 1, _orderStatusCreatedId);

        var orderId2 = Guid.NewGuid();
        await AddOrder(orderId2, 1, _orderStatusCompletedId);

        // Act
        var result = await _orderService.GetOrdersByStatusAsync("Created");

        // Assert
        Assert.That(result.Items.Count, Is.EqualTo(1));
        Assert.That(result.Items.First().Id, Is.EqualTo(orderId1));
    }

    [Test]
    public async Task GetOrdersByStatusAsync_ReturnsEmptyPage_WhenNoMatchingOrders()
    {
        // Arrange - add order with "Created" status
        await AddOrder(Guid.NewGuid(), 1, _orderStatusCreatedId);

        // Act - query for a status with no orders
        var result = await _orderService.GetOrdersByStatusAsync("Failed");

        // Assert
        Assert.That(result.Items.Count, Is.EqualTo(0));
    }
}
