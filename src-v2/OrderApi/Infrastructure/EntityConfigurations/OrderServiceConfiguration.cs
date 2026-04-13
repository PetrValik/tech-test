using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderApi.Infrastructure.Entities;

namespace OrderApi.Infrastructure.EntityConfigurations;

/// <summary>
/// EF Core mapping configuration for the <see cref="OrderService"/> entity.
/// </summary>
internal sealed class OrderServiceConfiguration : IEntityTypeConfiguration<OrderService>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<OrderService> entity)
    {
        entity.ToTable("order_service");
        entity.Property(service => service.Id).HasColumnType("binary(16)");
        entity.Property(service => service.Name).IsRequired().HasMaxLength(100).IsUnicode(false);
    }
}
