using Microsoft.Extensions.Caching.Memory;

namespace Fluxora.Api.Auth;

/// <summary>
/// Caps how many distinct email addresses a single source IP may attempt to authenticate as
/// within a rolling window, independent of the per-IP total-request rate limiter.
///
/// The per-IP request limiter alone does not stop a targeted account-lockout denial of service:
/// with a generous per-IP budget, a single attacker can spread exactly enough wrong-password
/// attempts across several known email addresses to trip Identity's own lockout on each of them,
/// then repeat the spray every time the lockouts expire. This guard makes that spread itself the
/// throttled resource, while leaving a legitimate user's own repeated attempts against their own
/// single account unaffected (only new, previously-unseen target emails count against the cap).
/// </summary>
public interface ILoginAttemptGuard
{
    bool ShouldThrottle(string sourceIp, string email);
}

public sealed class LoginAttemptGuard(IMemoryCache cache) : ILoginAttemptGuard
{
    private const int MaxDistinctEmailsPerWindow = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public bool ShouldThrottle(string sourceIp, string email)
    {
        var cacheKey = $"login-attempt-guard:{sourceIp}";
        var targets = cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Window;
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        })!;

        lock (targets)
        {
            if (targets.Contains(email))
            {
                return false;
            }

            if (targets.Count >= MaxDistinctEmailsPerWindow)
            {
                return true;
            }

            targets.Add(email);
            return false;
        }
    }
}
