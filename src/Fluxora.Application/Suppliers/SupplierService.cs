using System.Text.Json;
using Fluxora.Application.Common;
using Fluxora.Domain.Suppliers;

namespace Fluxora.Application.Suppliers;

public class SupplierService(
    ISupplierRepository repository,
    IUnitOfWork unitOfWork,
    IAuditWriter auditWriter,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<SupplierDto>> ListAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var suppliers = await repository.ListAsync(search, isActive, page, pageSize, cancellationToken);
        return suppliers.Select(ToDto).ToList();
    }

    public async Task<SupplierDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var supplier = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Supplier), id);
        return ToDto(supplier);
    }

    public async Task<SupplierDto> CreateAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        if (await repository.DocumentExistsAsync(request.Document, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A supplier with document '{request.Document}' already exists.");
        }

        var supplier = Supplier.Create(request.Name, request.Document, request.Email, request.Phone);
        repository.Add(supplier);

        auditWriter.Record(
            action: "SupplierCreated",
            entityType: nameof(Supplier),
            entityId: supplier.Id,
            afterValues: JsonSerializer.Serialize(ToDto(supplier)),
            actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Supplier), id);

        var before = JsonSerializer.Serialize(ToDto(supplier));
        supplier.Update(request.Name, request.Email, request.Phone);

        auditWriter.Record(
            action: "SupplierUpdated",
            entityType: nameof(Supplier),
            entityId: supplier.Id,
            beforeValues: before,
            afterValues: JsonSerializer.Serialize(ToDto(supplier)),
            actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(supplier);
    }

    public async Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var supplier = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Supplier), id);

        if (active)
        {
            supplier.Activate();
        }
        else
        {
            supplier.Deactivate();
        }

        auditWriter.Record(
            action: active ? "SupplierActivated" : "SupplierDeactivated",
            entityType: nameof(Supplier),
            entityId: supplier.Id,
            actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static SupplierDto ToDto(Supplier supplier) => new(
        supplier.Id, supplier.Name, supplier.Document, supplier.Email, supplier.Phone,
        supplier.IsActive, supplier.CreatedAtUtc);
}
