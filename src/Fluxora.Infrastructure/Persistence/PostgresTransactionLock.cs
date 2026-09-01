using Fluxora.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Fluxora.Infrastructure.Persistence;

public sealed class PostgresTransactionLock(AppDbContext dbContext) : ITransactionLock
{
    public async Task AcquireAsync(string resource, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({resource}, 0))",
            cancellationToken);
    }
}
