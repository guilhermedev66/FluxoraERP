using Fluxora.Domain.Sales;

namespace Fluxora.Application.Sales;

public interface ISalesOrderRepository
{
    Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesOrder>> ListAsync(
        Guid? customerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);

    void Add(SalesOrder order);
}
