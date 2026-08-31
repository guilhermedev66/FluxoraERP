using Fluxora.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluxora.Infrastructure.Persistence.Configurations;

public class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable("SalesOrderLines");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Quantity).HasPrecision(19, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(19, 4);
        builder.Ignore(l => l.LineTotal);
    }
}
