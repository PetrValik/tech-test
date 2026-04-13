using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Data.Entities;

namespace Order.Data.EntityConfigurations;

/// <summary>
/// EF Core mapping configuration for the <see cref="OrderItem"/> entity.
/// </summary>
internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OrderItem> entity)
    {
        entity.ToTable("order_item");

        entity.HasIndex(item => item.OrderId)
            .HasDatabaseName("OrderId");

        entity.HasIndex(item => item.ProductId)
            .HasDatabaseName("ProductId");

        entity.HasIndex(item => item.ServiceId)
            .HasDatabaseName("ServiceId");

        entity.Property(item => item.Id).HasColumnType("binary(16)");

        entity.Property(item => item.OrderId)
            .IsRequired()
            .HasColumnType("binary(16)");

        entity.Property(item => item.ProductId)
            .IsRequired()
            .HasColumnType("binary(16)");

        entity.Property(item => item.Quantity).HasColumnType("int(11)");

        entity.Property(item => item.ServiceId)
            .IsRequired()
            .HasColumnType("binary(16)");

        entity.HasOne(item => item.Order)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("order_item_oifk_1");

        entity.HasOne(item => item.Product)
            .WithMany(product => product.OrderItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("order_product_oifk_1");

        entity.HasOne(item => item.Service)
            .WithMany(service => service.OrderItems)
            .HasForeignKey(item => item.ServiceId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("order_service_oifk_1");
    }
}
