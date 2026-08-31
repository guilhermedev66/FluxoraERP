using Fluxora.Application.Automation;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Fluxora.Infrastructure.Automation;

[DisallowConcurrentExecution]
public class OverdueProcessingJob(
    OverdueProcessingService service,
    ILogger<OverdueProcessingJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var result = await service.ProcessAsync(context.CancellationToken);
            logger.LogInformation(
                "Overdue processing completed for {BusinessDate}: {Receivables} receivables and {Payables} payables marked.",
                result.AsOf,
                result.ReceivablesMarked,
                result.PayablesMarked);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Overdue processing failed.");
            throw;
        }
    }
}
