using Fluxora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluxora.Infrastructure.Persistence.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts", table =>
            table.HasCheckConstraint("CK_Receipts_Amount_Positive", "\"Amount\" > 0"));

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Amount).HasPrecision(19, 2);

        builder.HasIndex(r => r.ReceivableId);
        builder.HasIndex(r => r.ReceivableInstallmentId);

        builder.HasOne<Receivable>()
            .WithMany()
            .HasForeignKey(r => r.ReceivableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ReceivableInstallment>()
            .WithMany()
            .HasForeignKey(r => r.ReceivableInstallmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
