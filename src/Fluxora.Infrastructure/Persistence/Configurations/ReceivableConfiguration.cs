using Fluxora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluxora.Infrastructure.Persistence.Configurations;

public class ReceivableConfiguration : IEntityTypeConfiguration<Receivable>
{
    public void Configure(EntityTypeBuilder<Receivable> builder)
    {
        builder.ToTable("Receivables");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TotalAmount).HasPrecision(19, 2);

        // One receivable per approved sale - enforced at the database, not just in application code.
        builder.HasIndex(r => r.SalesOrderId).IsUnique();

        builder.HasMany(r => r.Installments)
            .WithOne()
            .HasForeignKey(i => i.ReceivableId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
