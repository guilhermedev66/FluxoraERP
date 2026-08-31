using Fluxora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluxora.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", table =>
            table.HasCheckConstraint("CK_Payments_Amount_Positive", "\"Amount\" > 0"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount).HasPrecision(19, 2);

        builder.HasIndex(p => p.PayableId);
        builder.HasIndex(p => p.PayableInstallmentId);

        builder.HasOne<Payable>()
            .WithMany()
            .HasForeignKey(p => p.PayableId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PayableInstallment>()
            .WithMany()
            .HasForeignKey(p => p.PayableInstallmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
