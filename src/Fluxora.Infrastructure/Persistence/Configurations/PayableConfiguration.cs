using Fluxora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluxora.Infrastructure.Persistence.Configurations;

public class PayableConfiguration : IEntityTypeConfiguration<Payable>
{
    public void Configure(EntityTypeBuilder<Payable> builder)
    {
        builder.ToTable("Payables");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.TotalAmount).HasPrecision(19, 2);

        // One payable per confirmed purchase - enforced at the database, not just in application code.
        builder.HasIndex(p => p.PurchaseOrderId).IsUnique();

        builder.HasMany(p => p.Installments)
            .WithOne()
            .HasForeignKey(i => i.PayableId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
