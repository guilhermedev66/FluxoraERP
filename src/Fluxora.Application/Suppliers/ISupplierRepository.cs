using Fluxora.Domain.Suppliers;

namespace Fluxora.Application.Suppliers;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DocumentExistsAsync(string document, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Supplier>> ListAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(Supplier supplier);
}
