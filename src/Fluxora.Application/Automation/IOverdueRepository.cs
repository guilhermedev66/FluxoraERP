using Fluxora.Domain.Finance;

namespace Fluxora.Application.Automation;

public interface IOverdueRepository
{
    Task<IReadOnlyList<ReceivableInstallment>> GetPendingReceivablesDueBeforeAsync(
        DateOnly asOf, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PayableInstallment>> GetPendingPayablesDueBeforeAsync(
        DateOnly asOf, CancellationToken cancellationToken = default);
}
