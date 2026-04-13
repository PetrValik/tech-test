using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Data.Entities;

namespace Order.Data.EntityConfigurations;

/// <summary>
/// EF Core mapping configuration for the <see cref="OrderStatus"/> entity.
/// </summary>
internal sealed class OrderStatusConfiguration : IEntityTypeConfiguration<OrderStatus>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OrderStatus> entity)
    {
        entity.ToTable("order_status");

        entity.HasIndex(status => status.Name)
            .HasDatabaseName("Status");

        entity.Property(status => status.Id).HasColumnType("binary(16)");

        entity.Property(status => status.Name)
            .IsRequired()
            .HasMaxLength(20)
            .IsUnicode(false);
    }
}
