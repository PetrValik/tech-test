using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Order.Data;
using Order.Data.Entities;
using Order.Data.Repositories;
using System;
using System.Threading.Tasks;

namespace Order.Service.Tests;

/// <summary>
/// Abstract base class shared by all OrderService unit/integration test fixtures.
/// Creates an in-memory SQLite database, seeds reference data, and builds the
/// <see cref="OrderService"/> and <see cref="OrderRepository"/> under test.
/// </summary>
[TestFixture]
public abstract class ServiceTestBase
{
    /// <summary>
    /// The service under test.
    /// </summary>
    protected IOrderService _orderService = null!;

    /// <summary>
    /// The EF Core context backed by the in-memory SQLite connection.
    /// </summary>
    protected OrderContext _orderContext = null!;

    /// <summary>
    /// The open SQLite connection kept alive for the duration of the test.
    /// </summary>
    protected SqliteConnection _connection = null!;

    /// <summary>
    /// Byte-array identifier for the <c>Created</c> order status.
    /// </summary>
    protected readonly byte[] _orderStatusCreatedId = Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Byte-array identifier for the <c>Completed</c> order status.
    /// </summary>
    protected readonly byte[] _orderStatusCompletedId = Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Byte-array identifier for the Email order service.
    /// </summary>
    protected readonly byte[] _orderServiceEmailId = Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Byte-array identifier for the 100GB Mailbox product.
    /// </summary>
    protected readonly byte[] _orderProductEmailId = Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Byte-array identifier for the Basic Antivirus product.
    /// </summary>
    protected readonly byte[] _orderProductAntivirusId = Guid.NewGuid().ToByteArray();

    /// <summary>
    /// Creates the SQLite in-memory database, seeds reference data, and builds the service under test.
    /// </summary>
    [SetUp]
    public async Task Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<OrderContext>()
            .UseSqlite(_connection)
            .EnableDetailedErrors(true)
            .EnableSensitiveDataLogging(true)
            .Options;

        _orderContext = new OrderContext(options);
        await _orderContext.Database.EnsureCreatedAsync();

        var orderRepository = new OrderRepository(_orderContext);
        _orderService = new OrderService(orderRepository, NullLogger<OrderService>.Instance);
        await AddReferenceDataAsync(_orderContext);
    }

    /// <summary>
    /// Disposes the EF Core context and SQLite connection after each test.
    /// </summary>
    [TearDown]
    public async Task TearDown()
    {
        await _orderContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    /// <summary>
    /// Inserts a single order with one Email-product item into the database.
    /// </summary>
    /// <param name="orderId">The identifier to assign to the new order.</param>
    /// <param name="quantity">Number of units in the order item.</param>
    /// <param name="statusId">Status byte array; defaults to <see cref="_orderStatusCreatedId"/>.</param>
    protected async Task AddOrder(Guid orderId, int quantity, byte[]? statusId = null)
    {
        var orderIdBytes = orderId.ToByteArray();
        _orderContext.Orders.Add(new Data.Entities.Order
        {
            Id          = orderIdBytes,
            ResellerId  = Guid.NewGuid().ToByteArray(),
            CustomerId  = Guid.NewGuid().ToByteArray(),
            CreatedDate = DateTime.UtcNow,
            StatusId    = statusId ?? _orderStatusCreatedId,
        });

        _orderContext.OrderItems.Add(new Data.Entities.OrderItem
        {
            Id        = Guid.NewGuid().ToByteArray(),
            OrderId   = orderIdBytes,
            ServiceId = _orderServiceEmailId,
            ProductId = _orderProductEmailId,
            Quantity  = quantity
        });

        await _orderContext.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds statuses, services, and products required by all test fixtures.
    /// </summary>
    private async Task AddReferenceDataAsync(OrderContext orderContext)
    {
        orderContext.OrderStatuses.Add(new OrderStatus { Id = _orderStatusCreatedId,   Name = "Created"     });
        orderContext.OrderStatuses.Add(new OrderStatus { Id = _orderStatusCompletedId, Name = "Completed"   });
        orderContext.OrderStatuses.Add(new OrderStatus { Id = Guid.NewGuid().ToByteArray(), Name = "In Progress" });
        orderContext.OrderStatuses.Add(new OrderStatus { Id = Guid.NewGuid().ToByteArray(), Name = "Failed"      });

        orderContext.OrderServices.Add(new Data.Entities.OrderService
        {
            Id   = _orderServiceEmailId,
            Name = "Email"
        });

        orderContext.OrderProducts.Add(new OrderProduct
        {
            Id        = _orderProductEmailId,
            Name      = "100GB Mailbox",
            UnitCost  = 0.8m,
            UnitPrice = 0.9m,
            ServiceId = _orderServiceEmailId
        });

        var antivirusServiceId = Guid.NewGuid().ToByteArray();
        orderContext.OrderServices.Add(new Data.Entities.OrderService
        {
            Id   = antivirusServiceId,
            Name = "Antivirus"
        });

        orderContext.OrderProducts.Add(new OrderProduct
        {
            Id        = _orderProductAntivirusId,
            Name      = "Basic Antivirus",
            UnitCost  = 1.5m,
            UnitPrice = 2.0m,
            ServiceId = antivirusServiceId
        });

        await orderContext.SaveChangesAsync();
    }
}
