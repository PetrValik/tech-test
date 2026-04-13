using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderApi.Infrastructure.Entities;

namespace OrderApi.Infrastructure.EntityConfigurations;

/// <summary>
/// EF Core mapping configuration for the <see cref="Entities.Order"/> entity.
/// </summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Entities.Order>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Entities.Order> entity)
    {
        entity.ToTable("order");
        entity.HasQueryFilter(order => !order.IsDeleted);
        entity.HasIndex(order => order.CreatedDate).HasDatabaseName("CreatedDate").IsDescending();
        entity.HasIndex(order => order.CustomerId).HasDatabaseName("CustomerId");
        entity.HasIndex(order => order.StatusId).HasDatabaseName("StatusId");
        entity.HasIndex(order => order.ResellerId).HasDatabaseName("ResellerId");
        entity.Property(order => order.Id).HasColumnType("binary(16)");
        entity.Property(order => order.CustomerId).IsRequired().HasColumnType("binary(16)");
        entity.Property(order => order.ResellerId).IsRequired().HasColumnType("binary(16)");
        entity.Property(order => order.StatusId).IsRequired().HasColumnType("binary(16)");
        entity.Property(order => order.IsDeleted).HasDefaultValue(false);
        entity.Property(order => order.DeletedAt).IsRequired(false);
        entity.Property(order => order.ConcurrencyStamp)
            .IsRequired()
            .HasMaxLength(32)
            .IsConcurrencyToken()
            .HasDefaultValueSql("(REPLACE(UUID(), '-', ''))");
        entity.HasOne(order => order.Status).WithMany(status => status.Orders)
            .HasForeignKey(order => order.StatusId).OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("order_ofk_1");
    }
}
