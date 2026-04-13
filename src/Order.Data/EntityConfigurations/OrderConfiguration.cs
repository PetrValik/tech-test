using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Order.Data.EntityConfigurations;

/// <summary>
/// EF Core mapping configuration for the <see cref="Entities.Order"/> entity.
/// </summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Entities.Order>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Entities.Order> entity)
    {
        entity.ToTable("order");

        entity.HasIndex(order => order.CustomerId)
            .HasDatabaseName("CustomerId");

        entity.HasIndex(order => order.StatusId)
            .HasDatabaseName("StatusId");

        entity.HasIndex(order => order.ResellerId)
            .HasDatabaseName("ResellerId");

        entity.Property(order => order.Id).HasColumnType("binary(16)");

        entity.Property(order => order.CustomerId)
            .IsRequired()
            .HasColumnType("binary(16)");

        entity.Property(order => order.ResellerId)
            .IsRequired()
            .HasColumnType("binary(16)");

        entity.Property(order => order.StatusId)
            .IsRequired()
            .HasColumnType("binary(16)");

        entity.Property(order => order.ConcurrencyStamp)
            .IsRequired()
            .HasMaxLength(32)
            .IsConcurrencyToken();

        entity.HasOne(order => order.Status)
            .WithMany(status => status.Orders)
            .HasForeignKey(order => order.StatusId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("order_ofk_1");
    }
}
