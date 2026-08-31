using Fluxora.Domain.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluxora.Infrastructure.Persistence.Configurations;

public class ReceivableInstallmentConfiguration : IEntityTypeConfiguration<ReceivableInstallment>
{
    public void Configure(EntityTypeBuilder<ReceivableInstallment> builder)
    {
        builder.ToTable("ReceivableInstallments");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Amount).HasPrecision(19, 2);
        builder.Property(i => i.AmountPaid).HasPrecision(19, 2);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Version).IsConcurrencyToken();
        builder.Ignore(i => i.RemainingAmount);

        builder.HasIndex(i => new { i.ReceivableId, i.Number }).IsUnique();
        builder.HasIndex(i => i.DueDate);
    }
}
