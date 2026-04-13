using Microsoft.EntityFrameworkCore;
using Order.Data.Entities;

namespace Order.Data;

/// <summary>
/// EF Core DbContext for the Order domain.
/// Configured by the DI container via AddDbContext in Startup.
/// </summary>
public class OrderContext : DbContext
{
    /// <summary>
    /// Parameterless constructor retained for EF Core design-time tooling (migrations).
    /// </summary>
    public OrderContext()
    {
    }

    /// <summary>
    /// Creates a new context instance with the supplied options.
    /// </summary>
    /// <param name="options">Provider options including the connection string.</param>
    public OrderContext(DbContextOptions<OrderContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// All orders in the system.
    /// </summary>
    public DbSet<Entities.Order> Orders { get; set; }

    /// <summary>
    /// All order line items in the system.
    /// </summary>
    public DbSet<OrderItem> OrderItems { get; set; }

    /// <summary>
    /// Product catalogue.
    /// </summary>
    public DbSet<OrderProduct> OrderProducts { get; set; }

    /// <summary>
    /// Service category catalogue.
    /// </summary>
    public DbSet<OrderService> OrderServices { get; set; }

    /// <summary>
    /// All valid order status values.
    /// </summary>
    public DbSet<OrderStatus> OrderStatuses { get; set; }

    /// <summary>
    /// Applies all <see cref="IEntityTypeConfiguration{T}"/> classes found in this assembly.
    /// Each entity has its own configuration class under <c>EntityConfigurations/</c>.
    /// </summary>
    /// <param name="modelBuilder">The model builder provided by EF Core.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderContext).Assembly);
    }
}
