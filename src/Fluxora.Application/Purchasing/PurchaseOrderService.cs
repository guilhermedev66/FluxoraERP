using System.Text.Json;
using Fluxora.Application.Catalog;
using Fluxora.Application.Common;
using Fluxora.Application.Finance;
using Fluxora.Application.Suppliers;
using Fluxora.Domain.Finance;
using Fluxora.Domain.Purchasing;

namespace Fluxora.Application.Purchasing;

public class PurchaseOrderService(
    IPurchaseOrderRepository orderRepository,
    ISupplierRepository supplierRepository,
    IProductRepository productRepository,
    IPayableRepository payableRepository,
    IUnitOfWork unitOfWork,
    IAuditWriter auditWriter,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<PurchaseOrderDto>> ListAsync(
        Guid? supplierId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.ListAsync(supplierId, status, page, pageSize, cancellationToken);
        return orders.Select(ToDto).ToList();
    }

    public async Task<PurchaseOrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseOrder), id);
        return ToDto(order);
    }

    public async Task<PurchaseOrderDto> CreateDraftAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken)
            ?? throw new NotFoundException("Supplier", request.SupplierId);

        if (!supplier.IsActive)
        {
            throw new ConflictException($"Supplier '{supplier.Id}' is inactive and cannot receive new purchase orders.");
        }

        var order = PurchaseOrder.CreateDraft(request.SupplierId, currentUser.UserId ?? Guid.Empty);
        orderRepository.Add(order);

        auditWriter.Record("PurchaseOrderCreated", nameof(PurchaseOrder), order.Id, actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(order);
    }

    public async Task<PurchaseOrderDto> AddLineAsync(Guid orderId, AddPurchaseOrderLineRequest request, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseOrder), orderId);

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        order.AddLine(product.Id, product.Name, request.Quantity, request.UnitPrice);

        auditWriter.Record("PurchaseOrderLineAdded", nameof(PurchaseOrder), order.Id, actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(order);
    }

    public async Task<PurchaseOrderDto> ConfirmAsync(Guid orderId, ConfirmPurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseOrder), orderId);

        order.Confirm();

        var payable = Payable.Create(
            order.Id, order.SupplierId, order.Total, request.InstallmentCount, request.FirstDueDate, request.IntervalDays);
        payableRepository.Add(payable);

        auditWriter.Record(
            "PurchaseOrderConfirmed", nameof(PurchaseOrder), order.Id,
            afterValues: JsonSerializer.Serialize(new { order.Total, order.Version }), actorId: currentUser.UserId);
        auditWriter.Record(
            "PayableCreated", nameof(Payable), payable.Id,
            afterValues: JsonSerializer.Serialize(new { payable.PurchaseOrderId, payable.TotalAmount, InstallmentCount = payable.Installments.Count }),
            actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(order);
    }

    public async Task<PurchaseOrderDto> CancelAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseOrder), orderId);

        order.Cancel();

        auditWriter.Record("PurchaseOrderCancelled", nameof(PurchaseOrder), order.Id, actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(order);
    }

    private static PurchaseOrderDto ToDto(PurchaseOrder order) => new(
        order.Id, order.SupplierId, order.Status.ToString(), order.Total, order.ConfirmedAtUtc, order.Version,
        order.CreatedAtUtc, order.Lines.Select(l => new PurchaseOrderLineDto(
            l.Id, l.ProductId, l.ProductName, l.Quantity, l.UnitPrice, l.LineTotal)).ToList());
}
