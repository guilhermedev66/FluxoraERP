namespace Fluxora.Application.Purchasing;

public sealed record PurchaseOrderLineDto(Guid Id, Guid ProductId, string ProductName, decimal Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record PurchaseOrderDto(
    Guid Id, Guid SupplierId, string Status, decimal Total, DateTime? ConfirmedAtUtc, int Version,
    DateTime CreatedAtUtc, IReadOnlyList<PurchaseOrderLineDto> Lines);

public sealed record CreatePurchaseOrderRequest(Guid SupplierId);

// Unlike sales (which snapshots the catalog's selling price), purchasing takes the
// negotiated cost explicitly - purchase price is not the same as the product's sale price.
public sealed record AddPurchaseOrderLineRequest(Guid ProductId, decimal Quantity, decimal UnitPrice);

public sealed record ConfirmPurchaseOrderRequest(int InstallmentCount, DateOnly FirstDueDate, int IntervalDays = 30);
