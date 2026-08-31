namespace Fluxora.Application.Finance;

public sealed record InstallmentDto(
    Guid Id, int Number, DateOnly DueDate, decimal Amount, decimal AmountPaid, string Status, int Version);

public sealed record ReceivableDto(
    Guid Id, Guid SalesOrderId, Guid CustomerId, decimal TotalAmount, IReadOnlyList<InstallmentDto> Installments);

public sealed record PayableDto(
    Guid Id, Guid PurchaseOrderId, Guid SupplierId, decimal TotalAmount, IReadOnlyList<InstallmentDto> Installments);
