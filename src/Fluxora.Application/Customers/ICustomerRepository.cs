using Fluxora.Domain.Customers;

namespace Fluxora.Application.Customers;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DocumentExistsAsync(string document, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> ListAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    void Add(Customer customer);
}
