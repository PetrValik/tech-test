using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Order.Data;
using Order.Data.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Order.API.Tests.Helpers;

/// <summary>
/// Custom WebApplicationFactory{TEntryPoint} that replaces the MySQL
/// database with a shared SQLite in-memory connection so integration tests run
/// without any external infrastructure.
/// </summary>
/// <remarks>
/// A single SqliteConnection is kept open for the lifetime of the factory,
/// ensuring all service-scoped OrderContext instances share the same schema.
/// Each test resets the schema via ResetDatabase.
/// </remarks>
public class OrderApiFactory : WebApplicationFactory<Order.WebAPI.Program>
{
    /// <summary>
    /// Shared in-memory SQLite connection used by every OrderContext during tests.
    /// </summary>
    public SqliteConnection Connection { get; } = new SqliteConnection("Filename=:memory:");

    /// <summary>
    /// Opens the shared connection before the host is built.
    /// </summary>
    public OrderApiFactory()
    {
        Connection.Open();
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Provide a dummy MySQL connection string so Startup.ConfigureServices does not
        // throw when calling UseMySql(connectionString).
        builder.UseSetting("OrderConnectionString", "Server=localhost;Database=test;User=test;Password=test;");

        builder.ConfigureLogging(logging =>
        {
            logging.AddFilter(
                "Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware",
                LogLevel.Error);
        });

        builder.ConfigureServices(services =>
        {
            // Remove all OrderContext-related registrations added by Startup.
            // We check for any generic service type that has OrderContext as a type argument,
            // plus OrderContext and DbContextOptions<OrderContext> themselves.
            var orderContextType = typeof(OrderContext);
            var toRemove = services
                .Where(descriptor => descriptor.ServiceType == orderContextType
                         || descriptor.ServiceType == typeof(DbContextOptions<OrderContext>)
                         || (descriptor.ServiceType.IsGenericType
                             && descriptor.ServiceType.GetGenericArguments()
                                 .Any(argument => argument == orderContextType)))
                .ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            // Register a fresh DbContext backed by the shared SQLite connection.
            services.AddDbContext<OrderContext>(options =>
                options.UseSqlite(Connection));

            // Replace JWT Bearer with a test handler that auto-authenticates every request.
            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });
        });
    }

    /// <summary>
    /// Drops and recreates the schema, then seeds the reference data required by all tests.
    /// Call from [SetUp] to guarantee a clean state before each test.
    /// </summary>
    public async Task<SeedData> ResetDatabase()
    {
        await using var scope = Services.CreateAsyncScope();
        var orderContext = scope.ServiceProvider.GetRequiredService<OrderContext>();

        await orderContext.Database.EnsureDeletedAsync();
        await orderContext.Database.EnsureCreatedAsync();

        var seed = new SeedData();

        orderContext.OrderStatuses.AddRange(
            new OrderStatus { Id = seed.StatusCreatedId,    Name = "Created"     },
            new OrderStatus { Id = seed.StatusCompletedId,  Name = "Completed"   },
            new OrderStatus { Id = seed.StatusInProgressId, Name = "In Progress" },
            new OrderStatus { Id = seed.StatusFailedId,     Name = "Failed"      }
        );

        orderContext.OrderServices.Add(new Data.Entities.OrderService
        {
            Id   = seed.ServiceEmailId,
            Name = "Email"
        });

        orderContext.OrderProducts.Add(new OrderProduct
        {
            Id        = seed.ProductEmailId,
            Name      = "100GB Mailbox",
            UnitCost  = 0.8m,
            UnitPrice = 0.9m,
            ServiceId = seed.ServiceEmailId
        });

        orderContext.OrderServices.Add(new Data.Entities.OrderService
        {
            Id   = seed.ServiceAntivirusId,
            Name = "Antivirus"
        });

        orderContext.OrderProducts.Add(new OrderProduct
        {
            Id        = seed.ProductAntivirusId,
            Name      = "Premium Antivirus",
            UnitCost  = 1.5m,
            UnitPrice = 2.0m,
            ServiceId = seed.ServiceAntivirusId
        });

        await orderContext.SaveChangesAsync();
        return seed;
    }

    /// <summary>
    /// Adds a single order with one item to the database and returns the order Guid.
    /// </summary>
    /// <param name="seed">Reference-data identifiers returned by ResetDatabase.</param>
    /// <param name="quantity">Number of units in the order item.</param>
    /// <param name="statusId">Status byte array; defaults to StatusCreatedId.</param>
    /// <param name="createdDate">Creation timestamp; defaults to UtcNow.</param>
    /// <returns>The <see cref="Guid"/> of the newly created order.</returns>
    public async Task<Guid> AddOrder(SeedData seed, int quantity = 1, byte[]? statusId = null, DateTime? createdDate = null)
    {
        await using var scope = Services.CreateAsyncScope();
        var orderContext = scope.ServiceProvider.GetRequiredService<OrderContext>();

        var orderId = Guid.NewGuid();
        orderContext.Orders.Add(new Data.Entities.Order
        {
            Id          = orderId.ToByteArray(),
            ResellerId  = Guid.NewGuid().ToByteArray(),
            CustomerId  = Guid.NewGuid().ToByteArray(),
            StatusId    = statusId ?? seed.StatusCreatedId,
            CreatedDate = createdDate ?? DateTime.UtcNow
        });
        orderContext.OrderItems.Add(new Data.Entities.OrderItem
        {
            Id        = Guid.NewGuid().ToByteArray(),
            OrderId   = orderId.ToByteArray(),
            ProductId = seed.ProductEmailId,
            ServiceId = seed.ServiceEmailId,
            Quantity  = quantity
        });
        await orderContext.SaveChangesAsync();
        return orderId;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Connection.Dispose();
        }
    }
}

