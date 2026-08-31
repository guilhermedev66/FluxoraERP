using Fluxora.Domain.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fluxora.Infrastructure.Persistence.Configurations;

public class DashboardSnapshotConfiguration : IEntityTypeConfiguration<DashboardSnapshot>
{
    public void Configure(EntityTypeBuilder<DashboardSnapshot> builder)
    {
        builder.ToTable("DashboardSnapshots");
        builder.HasKey(snapshot => snapshot.Id);
        builder.HasIndex(snapshot => snapshot.BusinessDate).IsUnique();
        builder.Property(snapshot => snapshot.CurrentBalance).HasPrecision(19, 2);
        builder.Property(snapshot => snapshot.MonthRevenue).HasPrecision(19, 2);
        builder.Property(snapshot => snapshot.MonthExpenses).HasPrecision(19, 2);
        builder.Property(snapshot => snapshot.OverdueReceivablesAmount).HasPrecision(19, 2);
        builder.Property(snapshot => snapshot.OverduePayablesAmount).HasPrecision(19, 2);
    }
}
