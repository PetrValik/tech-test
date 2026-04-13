using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using OrderApi.Infrastructure;
using OrderApi.Infrastructure.Entities;

namespace OrderApi.Tests.Common;

/// <summary>
/// WebApplicationFactory that replaces the MySQL database with an in-process SQLite database.
/// A shared connection is used so the schema persists for the lifetime of the factory.
/// </summary>
public class OrderApiTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Represents the underlying SQLite database connection used for in-memory operations.
    /// </summary>
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    /// <summary>
    /// Opens the shared SQLite connection so it is not garbage-collected between requests.
    /// </summary>
    public Task InitializeAsync()
    {
        _connection.Open();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Closes the shared connection and disposes the factory when torn down.
    /// Uses 'new' because IAsyncLifetime requires Task while WebApplicationFactory uses ValueTask.
    /// </summary>
    public new async Task DisposeAsync()
    {
        _connection.Close();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Replaces the Pomelo MySQL DbContext registration with an SQLite registration
    /// pointing at the shared in-memory connection.
    /// </summary>
    /// <param name="builder">The IWebHostBuilder to configure.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(s => s.ServiceType == typeof(IDbContextOptionsConfiguration<OrderContext>))
                .ToList();
            toRemove.ForEach(s => services.Remove(s));

            services.AddDbContext<OrderContext>(options =>
                options.UseSqlite(_connection));

            // Replace the output cache store with a no-op to prevent cached responses
            // from leaking across tests. Each test resets the DB but the cache would
            // still serve stale data.
            var cacheStoreDescriptors = services
                .Where(s => s.ServiceType == typeof(IOutputCacheStore))
                .ToList();
            cacheStoreDescriptors.ForEach(s => services.Remove(s));
            services.AddSingleton<IOutputCacheStore, NoOpOutputCacheStore>();

            // Remove background cleanup services so they don't interfere with test data.
            var hostedService = services
                .Where(s => s.ImplementationType?.Name == "StaleOrderCleanupService"
                         || s.ImplementationType?.Name == "IdempotencyCleanupService")
                .ToList();
            hostedService.ForEach(s => services.Remove(s));

            // Replace JWT Bearer with a test handler that auto-authenticates every request.
            services.AddAuthentication("TestScheme")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", _ => { });
        });
    }

    /// <summary>
    /// Drops and recreates the schema, then seeds the four required OrderStatus rows.
    /// Call this at the start of each test to get a clean, predictable state.
    /// All tests can rely on the four statuses being present without seeding them manually.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        db.OrderStatuses.AddRange(
            new OrderStatus { Id = Guid.NewGuid().ToByteArray(), Name = OrderStatusNames.Created },
            new OrderStatus { Id = Guid.NewGuid().ToByteArray(), Name = OrderStatusNames.InProgress },
            new OrderStatus { Id = Guid.NewGuid().ToByteArray(), Name = OrderStatusNames.Failed },
            new OrderStatus { Id = Guid.NewGuid().ToByteArray(), Name = OrderStatusNames.Completed });
        await db.SaveChangesAsync();
    }
}
