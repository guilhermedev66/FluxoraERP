using Fluxora.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluxora.Infrastructure.Persistence.Configurations;

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Quantity).HasPrecision(19, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(19, 4);
        builder.Ignore(l => l.LineTotal);
    }
}
