using Fluxora.Domain.Finance;

namespace Fluxora.Application.Finance;

public interface IPayableRepository
{
    Task<Payable?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payable>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    void Add(Payable payable);

    void AddPayment(Payment payment);
}
