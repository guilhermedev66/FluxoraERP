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
        var receivableIds = await repository.GetPendingReceivableIdsDueBeforeAsync(asOf, cancellationToken);
        var payableIds = await repository.GetPendingPayableIdsDueBeforeAsync(asOf, cancellationToken);

        var receivablesMarked = await MarkReceivablesAsync(receivableIds, asOf, cancellationToken);
        var payablesMarked = await MarkPayablesAsync(payableIds, asOf, cancellationToken);

        return new OverdueProcessingResult(asOf, receivablesMarked, payablesMarked);
    }

    private async Task<int> MarkReceivablesAsync(
        IEnumerable<Guid> installmentIds, DateOnly asOf, CancellationToken cancellationToken)
    {
        var marked = 0;
        foreach (var installmentId in installmentIds)
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
            var transition = await repository.TryMarkReceivableOverdueAsync(
                installmentId, asOf, cancellationToken);
            if (transition is null)
            {
                continue;
            }

            auditWriter.Record(
                "ReceivableInstallmentMarkedOverdue",
                nameof(ReceivableInstallment),
                transition.InstallmentId,
                beforeValues: JsonSerializer.Serialize(new
                {
                    Status = InstallmentStatus.Pending,
                    Version = transition.VersionBefore,
                }),
                afterValues: JsonSerializer.Serialize(new
                {
                    Status = InstallmentStatus.Overdue,
                    Version = transition.VersionAfter,
                    asOf,
                }),
                actorType: ActorType.System);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            marked++;
        }

        return marked;
    }

    private async Task<int> MarkPayablesAsync(
        IEnumerable<Guid> installmentIds, DateOnly asOf, CancellationToken cancellationToken)
    {
        var marked = 0;
        foreach (var installmentId in installmentIds)
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
            var transition = await repository.TryMarkPayableOverdueAsync(
                installmentId, asOf, cancellationToken);
            if (transition is null)
            {
                continue;
            }

            auditWriter.Record(
                "PayableInstallmentMarkedOverdue",
                nameof(PayableInstallment),
                transition.InstallmentId,
                beforeValues: JsonSerializer.Serialize(new
                {
                    Status = InstallmentStatus.Pending,
                    Version = transition.VersionBefore,
                }),
                afterValues: JsonSerializer.Serialize(new
                {
                    Status = InstallmentStatus.Overdue,
                    Version = transition.VersionAfter,
                    asOf,
                }),
                actorType: ActorType.System);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            marked++;
        }

        return marked;
    }
}
