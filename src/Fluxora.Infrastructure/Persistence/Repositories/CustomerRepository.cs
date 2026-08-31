using Fluxora.Application.Customers;
using Fluxora.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence.Repositories;

public class CustomerRepository(AppDbContext dbContext) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> DocumentExistsAsync(string document, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        dbContext.Customers.AnyAsync(
            c => c.Document == document && (excludeId == null || c.Id != excludeId), cancellationToken);

    public async Task<IReadOnlyList<Customer>> ListAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(c => EF.Functions.ILike(c.Name, term) || EF.Functions.ILike(c.Document, term));
        }

        if (isActive is not null)
        {
            query = query.Where(c => c.IsActive == isActive);
        }

        return await query
            .OrderBy(c => c.Name)
            .Skip(Math.Max(0, page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public void Add(Customer customer) => dbContext.Customers.Add(customer);
}
