using Fluxora.Domain.Finance;

namespace Fluxora.Application.Finance;

public interface IReceivableRepository
{
    Task<Receivable?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForSalesOrderAsync(Guid salesOrderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Receivable>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    void Add(Receivable receivable);
}
