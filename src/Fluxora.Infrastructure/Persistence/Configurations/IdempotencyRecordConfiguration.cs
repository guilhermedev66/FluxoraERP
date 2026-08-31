using Fluxora.Infrastructure.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluxora.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Operation).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Key).IsRequired().HasMaxLength(128);
        builder.Property(r => r.RequestHash).IsRequired().HasMaxLength(64);
        builder.Property(r => r.ResponseBody).HasColumnType("jsonb");

        builder.HasIndex(r => new { r.Operation, r.Key }).IsUnique();
    }
}
