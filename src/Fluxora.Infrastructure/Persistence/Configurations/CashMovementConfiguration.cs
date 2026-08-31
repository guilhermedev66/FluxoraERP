using Fluxora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluxora.Infrastructure.Persistence.Configurations;

public class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("CashMovements");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Amount).HasPrecision(19, 2);
        builder.Property(c => c.Direction).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.ReferenceType).IsRequired().HasMaxLength(50);

        builder.HasIndex(c => c.OccurredAtUtc);
        builder.HasIndex(c => new { c.ReferenceType, c.ReferenceId });
    }
}
