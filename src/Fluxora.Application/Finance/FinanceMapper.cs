using Fluxora.Domain.Finance;

namespace Fluxora.Application.Finance;

internal static class FinanceMapper
{
    public static ReceivableDto ToDto(Receivable receivable) => new(
        receivable.Id, receivable.SalesOrderId, receivable.CustomerId, receivable.TotalAmount,
        receivable.Installments.Select(ToDto).ToList());

    public static PayableDto ToDto(Payable payable) => new(
        payable.Id, payable.PurchaseOrderId, payable.SupplierId, payable.TotalAmount,
        payable.Installments.Select(ToDto).ToList());

    private static InstallmentDto ToDto(ReceivableInstallment installment) => new(
        installment.Id, installment.Number, installment.DueDate, installment.Amount,
        installment.AmountPaid, installment.Status.ToString(), installment.Version);

    private static InstallmentDto ToDto(PayableInstallment installment) => new(
        installment.Id, installment.Number, installment.DueDate, installment.Amount,
        installment.AmountPaid, installment.Status.ToString(), installment.Version);
}
