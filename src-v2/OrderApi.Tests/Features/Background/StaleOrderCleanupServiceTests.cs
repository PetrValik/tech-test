using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OrderApi.Infrastructure;
using OrderApi.Infrastructure.Entities;
using OrderApi.Services;

namespace OrderApi.Tests.Features.Background;

/// <summary>
/// Integration tests for <see cref="StaleOrderCleanupService"/>.
/// Uses an isolated SQLite in-memory database — no WebApplicationFactory to avoid
/// Serilog frozen-logger conflicts when test classes run in parallel.
/// </summary>
public class StaleOrderCleanupServiceTests : IAsyncLifetime
{
    /// <summary>
    /// Shared in-memory SQLite connection kept open for the lifetime of the test class
    /// so the database schema persists across individual test runs.
    /// </summary>
    private SqliteConnection _connection = null!;

    /// <summary>
    /// Dependency-injection container built during <see cref="InitializeAsync"/>
    /// and shared by all test methods in this class.
    /// </summary>
    private IServiceProvider _services = null!;

    /// <summary>
    /// Raw byte-array ID for the <em>Created</em> order status seeded in <see cref="InitializeAsync"/>.
    /// </summary>
    private byte[] _createdStatusId = null!;

    /// <summary>
    /// Raw byte-array ID for the <em>Failed</em> order status seeded in <see cref="InitializeAsync"/>.
    /// </summary>
    private byte[] _failedStatusId = null!;

    /// <summary>
    /// Raw byte-array ID for the <em>Completed</em> order status seeded in <see cref="InitializeAsync"/>.
    /// </summary>
    private byte[] _completedStatusId = null!;

    /// <summary>
    /// Opens the shared SQLite connection, creates the schema, and seeds the three
    /// required order status rows (<em>Created</em>, <em>Failed</em>, <em>Completed</em>).
    /// Called automatically by xUnit before every test method.
    /// </summary>
    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<OrderContext>(options =>
            options.UseSqlite(_connection));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        _services = services.BuildServiceProvider();

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        await db.Database.EnsureCreatedAsync();

        _createdStatusId   = Guid.NewGuid().ToByteArray();
        _failedStatusId    = Guid.NewGuid().ToByteArray();
        _completedStatusId = Guid.NewGuid().ToByteArray();

        db.OrderStatuses.AddRange(
            new OrderStatus { Id = _createdStatusId,   Name = OrderStatusNames.Created   },
            new OrderStatus { Id = _failedStatusId,    Name = OrderStatusNames.Failed     },
            new OrderStatus { Id = _completedStatusId, Name = OrderStatusNames.Completed  });
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Disposes the service provider and closes the shared SQLite connection.
    /// Called automatically by xUnit after every test method.
    /// </summary>
    public async Task DisposeAsync()
    {
        await (_services as ServiceProvider)!.DisposeAsync();
        _connection.Dispose();
    }

    /// <summary>
    /// Constructs a <see cref="StaleOrderCleanupService"/> wired to the shared
    /// <see cref="_services"/> container and a null logger for silent test output.
    /// </summary>
    private StaleOrderCleanupService CreateService() =>
        new(_services,
            _services.GetRequiredService<IConfiguration>(),
            NullLogger<StaleOrderCleanupService>.Instance);

    /// <summary>
    /// Seeds a single order that was created 49 hours ago with <em>Created</em> status.
    /// After running the cleanup service the order must be transitioned to <em>Failed</em>.
    /// </summary>
    [Fact]
    public async Task CleanupService_CancelsOrdersOlderThan48Hours()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();

        var oldOrderId = Guid.NewGuid().ToByteArray();
        db.Orders.Add(new Order
        {
            Id          = oldOrderId,
            ResellerId  = Guid.NewGuid().ToByteArray(),
            CustomerId  = Guid.NewGuid().ToByteArray(),
            StatusId    = _createdStatusId,
            CreatedDate = DateTime.UtcNow.AddHours(-49)
        });
        await db.SaveChangesAsync();

        await CreateService().CleanupAsync(staleDays: 2, CancellationToken.None);

        using var verify = _services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<OrderContext>();
        var order = await verifyDb.Orders
            .IgnoreQueryFilters()
            .FirstAsync(o => o.Id == oldOrderId);

        Assert.True(order.StatusId.SequenceEqual(_failedStatusId));
    }

    /// <summary>
    /// Seeds a completed order that is 10 days old. After running cleanup the order's status
    /// must remain <em>Completed</em> because the service only processes <em>Created</em> orders.
    /// </summary>
    [Fact]
    public async Task CleanupService_SkipsOrdersWithNonCreatedStatus()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();

        var completedOrderId = Guid.NewGuid().ToByteArray();
        db.Orders.Add(new Order
        {
            Id          = completedOrderId,
            ResellerId  = Guid.NewGuid().ToByteArray(),
            CustomerId  = Guid.NewGuid().ToByteArray(),
            StatusId    = _completedStatusId,
            CreatedDate = DateTime.UtcNow.AddDays(-10)
        });
        await db.SaveChangesAsync();

        await CreateService().CleanupAsync(staleDays: 2, CancellationToken.None);

        using var verify = _services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<OrderContext>();
        var order = await verifyDb.Orders
            .IgnoreQueryFilters()
            .FirstAsync(o => o.Id == completedOrderId);

        Assert.True(order.StatusId.SequenceEqual(_completedStatusId),
            "Order with non-Created status should not be changed");
    }

    /// <summary>
    /// Seeds a <em>Created</em> order that is only 1 hour old (well within the 48-hour cutoff).
    /// After running cleanup the order's status must remain <em>Created</em>.
    /// </summary>
    [Fact]
    public async Task CleanupService_SkipsOrdersWithinCutoff()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();

        var recentOrderId = Guid.NewGuid().ToByteArray();
        db.Orders.Add(new Order
        {
            Id          = recentOrderId,
            ResellerId  = Guid.NewGuid().ToByteArray(),
            CustomerId  = Guid.NewGuid().ToByteArray(),
            StatusId    = _createdStatusId,
            CreatedDate = DateTime.UtcNow.AddHours(-1)
        });
        await db.SaveChangesAsync();

        await CreateService().CleanupAsync(staleDays: 2, CancellationToken.None);

        using var verify = _services.CreateScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<OrderContext>();
        var order = await verifyDb.Orders
            .IgnoreQueryFilters()
            .FirstAsync(o => o.Id == recentOrderId);

        Assert.True(order.StatusId.SequenceEqual(_createdStatusId),
            "Recently created order should not be cancelled");
    }

    /// <summary>
    /// Verifies that running the cleanup service against an empty order table completes
    /// without throwing an exception.
    /// </summary>
    [Fact]
    public async Task CleanupService_HandlesNoOrdersToClean()
    {
        // No orders seeded — should complete without exception
        var exception = await Record.ExceptionAsync(
            () => CreateService().CleanupAsync(staleDays: 2, CancellationToken.None));

        Assert.Null(exception);
    }
}
