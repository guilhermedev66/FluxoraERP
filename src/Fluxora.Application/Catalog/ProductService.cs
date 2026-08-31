using System.Text.Json;
using Fluxora.Application.Common;
using Fluxora.Domain.Catalog;

namespace Fluxora.Application.Catalog;

public class ProductService(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    IAuditWriter auditWriter,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<ProductDto>> ListAsync(
        string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var products = await repository.ListAsync(search, isActive, page, pageSize, cancellationToken);
        return products.Select(ToDto).ToList();
    }

    public async Task<ProductDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);
        return ToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (await repository.SkuExistsAsync(request.Sku, cancellationToken))
        {
            throw new ConflictException($"A product with SKU '{request.Sku}' already exists.");
        }

        var product = Product.Create(request.Sku, request.Name, request.Price);
        repository.Add(product);

        auditWriter.Record(
            action: "ProductCreated",
            entityType: nameof(Product),
            entityId: product.Id,
            afterValues: JsonSerializer.Serialize(ToDto(product)),
            actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        var before = JsonSerializer.Serialize(ToDto(product));
        product.Update(request.Name, request.Price);

        auditWriter.Record(
            action: "ProductUpdated",
            entityType: nameof(Product),
            entityId: product.Id,
            beforeValues: before,
            afterValues: JsonSerializer.Serialize(ToDto(product)),
            actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(product);
    }

    public async Task SetActiveAsync(Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), id);

        if (active)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

        auditWriter.Record(
            action: active ? "ProductActivated" : "ProductDeactivated",
            entityType: nameof(Product),
            entityId: product.Id,
            actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static ProductDto ToDto(Product product) =>
        new(product.Id, product.Sku, product.Name, product.Price, product.IsActive, product.CreatedAtUtc);
}
