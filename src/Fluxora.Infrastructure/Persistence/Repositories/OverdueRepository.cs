using Fluxora.Application.Automation;
using Fluxora.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence.Repositories;

public class OverdueRepository(AppDbContext dbContext) : IOverdueRepository
{
    public async Task<IReadOnlyList<Guid>> GetPendingReceivableIdsDueBeforeAsync(
        DateOnly asOf, CancellationToken cancellationToken = default) =>
        await dbContext.ReceivableInstallments
            .AsNoTracking()
            .Where(i => i.Status == InstallmentStatus.Pending && i.DueDate < asOf)
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.Id)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetPendingPayableIdsDueBeforeAsync(
        DateOnly asOf, CancellationToken cancellationToken = default) =>
        await dbContext.PayableInstallments
            .AsNoTracking()
            .Where(i => i.Status == InstallmentStatus.Pending && i.DueDate < asOf)
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.Id)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

    public async Task<OverdueTransition?> TryMarkReceivableOverdueAsync(
        Guid installmentId, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var affected = await dbContext.ReceivableInstallments
            .Where(i => i.Id == installmentId &&
                i.Status == InstallmentStatus.Pending && i.DueDate < asOf)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.Status, InstallmentStatus.Overdue)
                .SetProperty(i => i.Version, i => i.Version + 1), cancellationToken);

        if (affected == 0)
        {
            return null;
        }

        var versionAfter = await dbContext.ReceivableInstallments.AsNoTracking()
            .Where(i => i.Id == installmentId)
            .Select(i => i.Version)
            .SingleAsync(cancellationToken);
        return new OverdueTransition(installmentId, versionAfter - 1, versionAfter);
    }

    public async Task<OverdueTransition?> TryMarkPayableOverdueAsync(
        Guid installmentId, DateOnly asOf, CancellationToken cancellationToken = default)
    {
        var affected = await dbContext.PayableInstallments
            .Where(i => i.Id == installmentId &&
                i.Status == InstallmentStatus.Pending && i.DueDate < asOf)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.Status, InstallmentStatus.Overdue)
                .SetProperty(i => i.Version, i => i.Version + 1), cancellationToken);

        if (affected == 0)
        {
            return null;
        }

        var versionAfter = await dbContext.PayableInstallments.AsNoTracking()
            .Where(i => i.Id == installmentId)
            .Select(i => i.Version)
            .SingleAsync(cancellationToken);
        return new OverdueTransition(installmentId, versionAfter - 1, versionAfter);
    }
}
