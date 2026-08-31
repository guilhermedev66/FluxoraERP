namespace Fluxora.Application.Finance;

public sealed record ApplyPaymentRequest(decimal Amount, int ExpectedVersion);

public sealed record PaymentDto(
    Guid Id, Guid PayableId, Guid PayableInstallmentId, decimal Amount, DateTime PaidAtUtc,
    string InstallmentStatus, int InstallmentVersion, decimal InstallmentRemainingAmount);

public sealed record ApplyReceiptRequest(decimal Amount, int ExpectedVersion);

public sealed record ReceiptDto(
    Guid Id, Guid ReceivableId, Guid ReceivableInstallmentId, decimal Amount, DateTime ReceivedAtUtc,
    string InstallmentStatus, int InstallmentVersion, decimal InstallmentRemainingAmount);

public sealed record CashMovementDto(Guid Id, DateTime OccurredAtUtc, string Direction, decimal Amount, string ReferenceType, Guid ReferenceId);
