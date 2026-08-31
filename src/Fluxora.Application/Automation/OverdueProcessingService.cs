using System.Text.Json;
using Fluxora.Application.Common;
using Fluxora.Domain.Auditing;
using Fluxora.Domain.Finance;

namespace Fluxora.Application.Automation;

public sealed record OverdueProcessingResult(DateOnly AsOf, int ReceivablesMarked, int PayablesMarked);

public class OverdueProcessingService(
    IOverdueRepository repository,
    IBusinessClock businessClock,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    public async Task<OverdueProcessingResult> ProcessAsync(CancellationToken cancellationToken = default)
    {
        var asOf = businessClock.Today;
        var receivables = await repository.GetPendingReceivablesDueBeforeAsync(asOf, cancellationToken);
        var payables = await repository.GetPendingPayablesDueBeforeAsync(asOf, cancellationToken);

        var receivablesMarked = MarkReceivables(receivables, asOf);
        var payablesMarked = MarkPayables(payables, asOf);

        if (receivablesMarked > 0 || payablesMarked > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new OverdueProcessingResult(asOf, receivablesMarked, payablesMarked);
    }

    private int MarkReceivables(IEnumerable<ReceivableInstallment> installments, DateOnly asOf)
    {
        var marked = 0;
        foreach (var installment in installments)
        {
            var versionBefore = installment.Version;
            if (!installment.MarkOverdue(asOf))
            {
                continue;
            }

            auditWriter.Record(
                "ReceivableInstallmentMarkedOverdue",
                nameof(ReceivableInstallment),
                installment.Id,
                beforeValues: JsonSerializer.Serialize(new { Status = InstallmentStatus.Pending, Version = versionBefore }),
                afterValues: JsonSerializer.Serialize(new { installment.Status, installment.Version, asOf }),
                actorType: ActorType.System);
            marked++;
        }

        return marked;
    }

    private int MarkPayables(IEnumerable<PayableInstallment> installments, DateOnly asOf)
    {
        var marked = 0;
        foreach (var installment in installments)
        {
            var versionBefore = installment.Version;
            if (!installment.MarkOverdue(asOf))
            {
                continue;
            }

            auditWriter.Record(
                "PayableInstallmentMarkedOverdue",
                nameof(PayableInstallment),
                installment.Id,
                beforeValues: JsonSerializer.Serialize(new { Status = InstallmentStatus.Pending, Version = versionBefore }),
                afterValues: JsonSerializer.Serialize(new { installment.Status, installment.Version, asOf }),
                actorType: ActorType.System);
            marked++;
        }

        return marked;
    }
}
