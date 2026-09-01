using Fluxora.Application.Common;
using Fluxora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Idempotency;

public class IdempotencyStore(AppDbContext dbContext, ITransactionLock transactionLock) : IIdempotencyStore
{
    public Task AcquireLockAsync(string operation, string key, CancellationToken cancellationToken = default) =>
        transactionLock.AcquireAsync($"{operation}:{key}", cancellationToken);

    public async Task<IdempotentResponse?> FindAsync(string operation, string key, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Operation == operation && r.Key == key, cancellationToken);

        return record is null ? null : new IdempotentResponse(record.RequestHash, record.ResponseStatus, record.ResponseBody);
    }

    public void Stage(string operation, string key, string requestHash, int responseStatus, string responseBody)
    {
        dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Operation = operation,
            Key = key,
            RequestHash = requestHash,
            ResponseStatus = responseStatus,
            ResponseBody = responseBody,
        });
    }
}
