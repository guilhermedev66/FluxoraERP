using Fluxora.Domain.Purchasing;

namespace Fluxora.Application.Purchasing;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseOrder>> ListAsync(
        Guid? supplierId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);

    void Add(PurchaseOrder order);
}
