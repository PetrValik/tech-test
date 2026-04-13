using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderApi.Infrastructure;
using OrderApi.Infrastructure.Entities;
using OrderApi.Tests.Common;

namespace OrderApi.Tests.Features.Orders;

/// <summary>
/// Abstract base class for order endpoint integration tests.
/// Provides the shared HTTP client, factory fixture, database reset lifecycle, and seed helpers.
/// Derived classes join the "Orders" xUnit collection so they share one <see cref="OrderApiTestFactory"/>
/// instance and run sequentially, avoiding concurrent host-initialisation failures.
/// Each derived class covers one feature area and declares its own test methods.
/// </summary>
public abstract class OrdersEndpointTestBase : IAsyncLifetime
{
    /// <summary>The shared application factory; one instance per derived test class.</summary>
    protected readonly OrderApiTestFactory Factory;

    /// <summary>An HTTP client configured to call the in-process test server.</summary>
    protected readonly HttpClient Client;

    /// <summary>
    /// Receives the shared factory injected by xUnit and creates an HTTP client for this test class.
    /// </summary>
    /// <param name="factory">The <see cref="OrderApiTestFactory"/> fixture shared across tests in this class.</param>
    protected OrdersEndpointTestBase(OrderApiTestFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Resets the database before every test for full isolation.
    /// </summary>
    public Task InitializeAsync() => Factory.ResetDatabaseAsync();

    /// <summary>
    /// No per-test teardown needed; factory handles disposal.
    /// </summary>
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Seeds the required lookup data (service, product, statuses) and returns their identifiers
    /// so individual tests can build order payloads without duplicating setup code.
    /// </summary>
    /// <param name="db">The <see cref="OrderContext"/> to write seed data into.</param>
    /// <returns>
    /// A tuple containing the generated reseller ID, customer ID, created-status ID, product ID, and service ID.
    /// </returns>
    protected static async Task<(Guid resellerId, Guid customerId, Guid statusId, Guid productId, Guid serviceId)>
        SeedReferenceDataAsync(OrderContext db)
    {
        var resellerId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        // Statuses are seeded by ResetDatabaseAsync — just query for Created.
        var createdStatus = await db.OrderStatuses.FirstAsync(s => s.Name == OrderStatusNames.Created);
        var statusId = new Guid(createdStatus.Id);

        db.OrderServices.Add(new OrderService { Id = serviceId.ToByteArray(), Name = "Email" });

        db.OrderProducts.Add(new OrderProduct
        {
            Id = productId.ToByteArray(),
            ServiceId = serviceId.ToByteArray(),
            Name = "100GB Mailbox",
            UnitCost = 0.8m,
            UnitPrice = 0.9m
        });

        await db.SaveChangesAsync();
        return (resellerId, customerId, statusId, productId, serviceId);
    }

    /// <summary>
    /// Creates a single order with one line item and persists it to the database.
    /// </summary>
    /// <param name="db">The <see cref="OrderContext"/> to write the order into.</param>
    /// <param name="statusId">The status to assign to the new order.</param>
    /// <param name="productId">The product to include as a line item.</param>
    /// <param name="serviceId">The service the product belongs to.</param>
    /// <param name="quantity">The quantity of the line item.</param>
    /// <returns>The identifier of the newly created order.</returns>
    protected static async Task<Guid> SeedOrderAsync(
        OrderContext db, Guid statusId, Guid productId, Guid serviceId, int quantity)
    {
        var orderId = Guid.NewGuid();

        db.Orders.Add(new Infrastructure.Entities.Order
        {
            Id = orderId.ToByteArray(),
            ResellerId = Guid.NewGuid().ToByteArray(),
            CustomerId = Guid.NewGuid().ToByteArray(),
            StatusId = statusId.ToByteArray(),
            CreatedDate = DateTime.UtcNow
        });

        db.OrderItems.Add(new OrderItem
        {
            Id = Guid.NewGuid().ToByteArray(),
            OrderId = orderId.ToByteArray(),
            ProductId = productId.ToByteArray(),
            ServiceId = serviceId.ToByteArray(),
            Quantity = quantity
        });

        await db.SaveChangesAsync();
        return orderId;
    }
}
