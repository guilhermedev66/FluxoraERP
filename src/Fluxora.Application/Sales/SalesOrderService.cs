using System.Text.Json;
using Fluxora.Application.Catalog;
using Fluxora.Application.Common;
using Fluxora.Application.Customers;
using Fluxora.Application.Finance;
using Fluxora.Domain.Finance;
using Fluxora.Domain.Sales;

namespace Fluxora.Application.Sales;

public class SalesOrderService(
    ISalesOrderRepository orderRepository,
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    IReceivableRepository receivableRepository,
    IUnitOfWork unitOfWork,
    IAuditWriter auditWriter,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<SalesOrderDto>> ListAsync(
        Guid? customerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var orders = await orderRepository.ListAsync(customerId, status, page, pageSize, cancellationToken);
        return orders.Select(ToDto).ToList();
    }

    public async Task<SalesOrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesOrder), id);
        return ToDto(order);
    }

    public async Task<SalesOrderDto> CreateDraftAsync(CreateSalesOrderRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId);

        if (!customer.IsActive)
        {
            throw new ConflictException($"Customer '{customer.Id}' is inactive and cannot receive new sales orders.");
        }

        var order = SalesOrder.CreateDraft(request.CustomerId, currentUser.UserId ?? Guid.Empty);
        orderRepository.Add(order);

        auditWriter.Record("SalesOrderCreated", nameof(SalesOrder), order.Id, actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(order);
    }

    public async Task<SalesOrderDto> AddLineAsync(Guid orderId, AddSalesOrderLineRequest request, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesOrder), orderId);

        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product", request.ProductId);

        if (!product.IsActive)
        {
            throw new ConflictException($"Product '{product.Id}' is inactive and cannot be added to a sales order.");
        }

        order.AddLine(product.Id, product.Name, request.Quantity, product.Price);

        auditWriter.Record("SalesOrderLineAdded", nameof(SalesOrder), order.Id, actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(order);
    }

    public async Task<SalesOrderDto> ApproveAsync(Guid orderId, ApproveSalesOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesOrder), orderId);

        var before = JsonSerializer.Serialize(new { order.Status, order.Version });
        order.Approve();

        var receivable = Receivable.Create(
            order.Id, order.CustomerId, order.Total, request.InstallmentCount, request.FirstDueDate, request.IntervalDays);
        receivableRepository.Add(receivable);

        auditWriter.Record(
            "SalesOrderApproved", nameof(SalesOrder), order.Id,
            beforeValues: before,
            afterValues: JsonSerializer.Serialize(new { order.Status, order.Total, order.Version }),
            actorId: currentUser.UserId);
        auditWriter.Record(
            "ReceivableCreated", nameof(Receivable), receivable.Id,
            afterValues: JsonSerializer.Serialize(new { receivable.SalesOrderId, receivable.TotalAmount, InstallmentCount = receivable.Installments.Count }),
            actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(order);
    }

    public async Task<SalesOrderDto> CancelAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(SalesOrder), orderId);

        var before = JsonSerializer.Serialize(new { order.Status, order.Version });
        order.Cancel();

        auditWriter.Record(
            "SalesOrderCancelled",
            nameof(SalesOrder),
            order.Id,
            beforeValues: before,
            afterValues: JsonSerializer.Serialize(new { order.Status, order.Version }),
            actorId: currentUser.UserId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(order);
    }

    private static SalesOrderDto ToDto(SalesOrder order) => new(
        order.Id, order.CustomerId, order.Status.ToString(), order.Total, order.ApprovedAtUtc, order.Version,
        order.CreatedAtUtc, order.Lines.Select(l => new SalesOrderLineDto(
            l.Id, l.ProductId, l.ProductName, l.Quantity, l.UnitPrice, l.LineTotal)).ToList());
}
