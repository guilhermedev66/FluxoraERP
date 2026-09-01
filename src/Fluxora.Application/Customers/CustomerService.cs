using System.Text.Json;
using Fluxora.Application.Common;
using Fluxora.Domain.Customers;

namespace Fluxora.Application.Customers;

public class CustomerService(
    ICustomerRepository repository,
    IUnitOfWork unitOfWork,
    ITransactionLock transactionLock,
    IAuditWriter auditWriter,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<CustomerDto>> ListAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var customers = await repository.ListAsync(search, isActive, page, pageSize, cancellationToken);
        return customers.Select(ToDto).ToList();
    }

    public async Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), id);
        return ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = Customer.Create(request.Name, request.Document, request.Email, request.Phone);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        await transactionLock.AcquireAsync($"customer-document:{customer.Document}", cancellationToken);

        if (await repository.DocumentExistsAsync(customer.Document, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A customer with document '{customer.Document}' already exists.");
        }

        repository.Add(customer);

        auditWriter.Record(
            action: "CustomerCreated",
            entityType: nameof(Customer),
            entityId: customer.Id,
            afterValues: JsonSerializer.Serialize(ToDto(customer)),
            actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), id);

        var before = JsonSerializer.Serialize(ToDto(customer));
        customer.Update(request.Name, request.Email, request.Phone);

        auditWriter.Record(
            action: "CustomerUpdated",
            entityType: nameof(Customer),
            entityId: customer.Id,
            beforeValues: before,
            afterValues: JsonSerializer.Serialize(ToDto(customer)),
            actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(customer);
    }

    public async Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var customer = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), id);

        if (active)
        {
            customer.Activate();
        }
        else
        {
            customer.Deactivate();
        }

        auditWriter.Record(
            action: active ? "CustomerActivated" : "CustomerDeactivated",
            entityType: nameof(Customer),
            entityId: customer.Id,
            actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static CustomerDto ToDto(Customer customer) => new(
        customer.Id, customer.Name, customer.Document, customer.Email, customer.Phone,
        customer.IsActive, customer.CreatedAtUtc);
}
