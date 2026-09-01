namespace Fluxora.Application.Automation;

public sealed record OverdueTransition(Guid InstallmentId, int VersionBefore, int VersionAfter);

public interface IOverdueRepository
{
    Task<IReadOnlyList<Guid>> GetPendingReceivableIdsDueBeforeAsync(
        DateOnly asOf, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetPendingPayableIdsDueBeforeAsync(
        DateOnly asOf, CancellationToken cancellationToken = default);

    Task<OverdueTransition?> TryMarkReceivableOverdueAsync(
        Guid installmentId, DateOnly asOf, CancellationToken cancellationToken = default);

    Task<OverdueTransition?> TryMarkPayableOverdueAsync(
        Guid installmentId, DateOnly asOf, CancellationToken cancellationToken = default);
}
