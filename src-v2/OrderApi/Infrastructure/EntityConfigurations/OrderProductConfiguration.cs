using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderApi.Infrastructure.Entities;

namespace OrderApi.Infrastructure.EntityConfigurations;

/// <summary>
/// EF Core mapping configuration for the <see cref="OrderProduct"/> entity.
/// </summary>
internal sealed class OrderProductConfiguration : IEntityTypeConfiguration<OrderProduct>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OrderProduct> entity)
    {
        entity.ToTable("order_product");
        entity.HasIndex(product => product.ServiceId).HasDatabaseName("order_service_opfk_1");
        entity.Property(product => product.Id).HasColumnType("binary(16)");
        entity.Property(product => product.ServiceId).IsRequired().HasColumnType("binary(16)");
        entity.Property(product => product.Name).IsRequired().HasMaxLength(100).IsUnicode(false);
        entity.Property(product => product.UnitCost).HasPrecision(10, 4);
        entity.Property(product => product.UnitPrice).HasPrecision(10, 4);
        entity.HasOne(product => product.Service).WithMany(service => service.OrderProducts)
            .HasForeignKey(product => product.ServiceId).OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("order_service_opfk_1");
    }
}
