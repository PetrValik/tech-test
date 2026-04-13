using Microsoft.EntityFrameworkCore;
using OrderApi.Infrastructure.Entities;

namespace OrderApi.Infrastructure;

/// <summary>
/// EF Core database context for the Order API.
/// Owns all five entity sets and maps them to the MySQL schema defined in mysql-init.sql.
/// </summary>
/// <param name="options">The EF Core options used to configure the database provider and connection.</param>
public class OrderContext(DbContextOptions<OrderContext> options): DbContext(options)
{
    /// <summary>
    /// All orders in the system.
    /// </summary>
    public DbSet<Entities.Order> Orders { get; set; }

    /// <summary>
    /// All order line items.
    /// </summary>
    public DbSet<OrderItem> OrderItems { get; set; }

    /// <summary>
    /// All purchasable products.
    /// </summary>
    public DbSet<OrderProduct> OrderProducts { get; set; }

    /// <summary>
    /// All service categories.
    /// </summary>
    public DbSet<OrderService> OrderServices { get; set; }

    /// <summary>
    /// All valid order statuses.
    /// </summary>
    public DbSet<OrderStatus> OrderStatuses { get; set; }

    /// <summary>
    /// Audit trail of order status transitions.
    /// </summary>
    public DbSet<OrderStatusHistory> StatusHistory { get; set; }

    /// <summary>
    /// Idempotency records for POST request deduplication.
    /// </summary>
    public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }

    /// <summary>
    /// Applies all <see cref="IEntityTypeConfiguration{T}"/> classes found in this assembly.
    /// Each entity has its own configuration class under Infrastructure/EntityConfigurations/.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderContext).Assembly);
    }
}
