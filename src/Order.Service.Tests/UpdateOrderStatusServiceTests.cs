using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Order.Model;
using System;
using System.Threading.Tasks;

namespace Order.Service.Tests;

/// <summary>
/// Unit/integration tests for <see cref="IOrderService.UpdateOrderStatusAsync"/> using a real
/// SQLite in-memory database to exercise the repository and service layers together.
/// </summary>
[TestFixture]
public class UpdateOrderStatusServiceTests : ServiceTestBase
{
    [Test]
    public async Task UpdateOrderStatusAsync_ReturnsSuccess_WhenOrderAndStatusExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        await AddOrder(orderId, 1);

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(orderId, "Completed");

        // Assert
        Assert.That(result, Is.EqualTo(UpdateOrderStatusResult.Success));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ReturnsOrderNotFound_WhenOrderDoesNotExist()
    {
        // Act
        var result = await _orderService.UpdateOrderStatusAsync(Guid.NewGuid(), "Completed");

        // Assert
        Assert.That(result, Is.EqualTo(UpdateOrderStatusResult.OrderNotFound));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ReturnsInvalidStatus_WhenStatusDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        await AddOrder(orderId, 1);

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(orderId, "NonExistentStatus");

        // Assert
        Assert.That(result, Is.EqualTo(UpdateOrderStatusResult.InvalidStatus));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ReturnsConcurrencyConflict_WhenOrderModifiedConcurrently()
    {
        // Arrange — create order (entity stays tracked in _orderContext)
        var orderId = Guid.NewGuid();
        await AddOrder(orderId, 1);

        // Simulate a concurrent modification by another request —
        // change the ConcurrencyStamp directly in the DB, bypassing the change tracker.
        var orderIdBytes = orderId.ToByteArray();
        await _orderContext.Database.ExecuteSqlRawAsync(
            "UPDATE \"order\" SET ConcurrencyStamp = 'concurrent-change' WHERE Id = {0}",
            orderIdBytes);

        // Act — the service loads the tracked entity (old stamp) and tries to save
        var result = await _orderService.UpdateOrderStatusAsync(orderId, "Completed");

        // Assert
        Assert.That(result, Is.EqualTo(UpdateOrderStatusResult.ConcurrencyConflict));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ActuallyChangesStatus()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        await AddOrder(orderId, 1, _orderStatusCreatedId);

        // Act
        await _orderService.UpdateOrderStatusAsync(orderId, "Completed");

        // Assert - verify the order's status actually changed
        var order = await _orderService.GetOrderByIdAsync(orderId);
        Assert.That(order, Is.Not.Null);
        Assert.That(order!.StatusName, Is.EqualTo("Completed"));
    }
}
