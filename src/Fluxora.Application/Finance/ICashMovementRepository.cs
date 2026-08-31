using Fluxora.Domain.Finance;

namespace Fluxora.Application.Finance;

public interface ICashMovementRepository
{
    void Add(CashMovement movement);

    Task<IReadOnlyList<CashMovement>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
