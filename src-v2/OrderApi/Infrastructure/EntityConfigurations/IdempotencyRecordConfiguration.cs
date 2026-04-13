using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderApi.Infrastructure.Entities;

namespace OrderApi.Infrastructure.EntityConfigurations;

/// <summary>
/// EF Core mapping configuration for the <see cref="IdempotencyRecord"/> entity.
/// </summary>
internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<IdempotencyRecord> entity)
    {
        entity.ToTable("idempotency_record");
        entity.HasKey(record => record.Key);
        entity.Property(record => record.Key).HasMaxLength(64);
        entity.Property(record => record.ResponseBody).IsRequired();
        entity.HasIndex(record => record.CreatedAt).HasDatabaseName("IdempotencyRecord_CreatedAt");
    }
}
