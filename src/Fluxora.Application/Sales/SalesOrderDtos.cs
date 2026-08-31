namespace Fluxora.Application.Sales;

public sealed record SalesOrderLineDto(Guid Id, Guid ProductId, string ProductName, decimal Quantity, decimal UnitPrice, decimal LineTotal);

public sealed record SalesOrderDto(
    Guid Id, Guid CustomerId, string Status, decimal Total, DateTime? ApprovedAtUtc, int Version,
    DateTime CreatedAtUtc, IReadOnlyList<SalesOrderLineDto> Lines);

public sealed record CreateSalesOrderRequest(Guid CustomerId);

public sealed record AddSalesOrderLineRequest(Guid ProductId, decimal Quantity);

public sealed record ApproveSalesOrderRequest(int InstallmentCount, DateOnly FirstDueDate, int IntervalDays = 30);
