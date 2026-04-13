using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderApi.Infrastructure.Entities;

namespace OrderApi.Infrastructure.EntityConfigurations;

/// <summary>
/// EF Core mapping configuration for the <see cref="OrderStatusHistory"/> entity.
/// </summary>
internal sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OrderStatusHistory> entity)
    {
        entity.ToTable("order_status_history");
        entity.Property(history => history.Id).HasColumnType("binary(16)");
        entity.Property(history => history.OrderId).IsRequired().HasColumnType("binary(16)");
        entity.Property(history => history.FromStatusId).IsRequired().HasColumnType("binary(16)");
        entity.Property(history => history.ToStatusId).IsRequired().HasColumnType("binary(16)");
        entity.HasIndex(history => history.OrderId).HasDatabaseName("OrderStatusHistory_OrderId");
        entity.HasOne(history => history.Order).WithMany()
            .HasForeignKey(history => history.OrderId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(history => history.FromStatus).WithMany()
            .HasForeignKey(history => history.FromStatusId).OnDelete(DeleteBehavior.ClientSetNull);
        entity.HasOne(history => history.ToStatus).WithMany()
            .HasForeignKey(history => history.ToStatusId).OnDelete(DeleteBehavior.ClientSetNull);
    }
}
