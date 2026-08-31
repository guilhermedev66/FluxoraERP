using Fluxora.Application.Automation;
using Fluxora.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence.Repositories;

public class OverdueRepository(AppDbContext dbContext) : IOverdueRepository
{
    public async Task<IReadOnlyList<ReceivableInstallment>> GetPendingReceivablesDueBeforeAsync(
        DateOnly asOf, CancellationToken cancellationToken = default) =>
        await dbContext.ReceivableInstallments
            .Where(i => i.Status == InstallmentStatus.Pending && i.DueDate < asOf)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PayableInstallment>> GetPendingPayablesDueBeforeAsync(
        DateOnly asOf, CancellationToken cancellationToken = default) =>
        await dbContext.PayableInstallments
            .Where(i => i.Status == InstallmentStatus.Pending && i.DueDate < asOf)
            .OrderBy(i => i.DueDate)
            .ToListAsync(cancellationToken);
}
