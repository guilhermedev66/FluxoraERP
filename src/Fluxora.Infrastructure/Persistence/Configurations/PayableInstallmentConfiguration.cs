using Fluxora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluxora.Infrastructure.Persistence.Configurations;

public class PayableInstallmentConfiguration : IEntityTypeConfiguration<PayableInstallment>
{
    public void Configure(EntityTypeBuilder<PayableInstallment> builder)
    {
        builder.ToTable("PayableInstallments", table =>
        {
            table.HasCheckConstraint("CK_PayableInstallments_Amount_Positive", "\"Amount\" > 0");
            table.HasCheckConstraint(
                "CK_PayableInstallments_AmountPaid_Range",
                "\"AmountPaid\" >= 0 AND \"AmountPaid\" <= \"Amount\"");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Amount).HasPrecision(19, 2);
        builder.Property(i => i.AmountPaid).HasPrecision(19, 2);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Version).IsConcurrencyToken();
        builder.Ignore(i => i.RemainingAmount);

        builder.HasIndex(i => new { i.PayableId, i.Number }).IsUnique();
        builder.HasIndex(i => i.DueDate);
    }
}
