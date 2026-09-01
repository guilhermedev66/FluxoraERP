using Microsoft.Extensions.Caching.Memory;

namespace Fluxora.Api.Auth;

/// <summary>
/// Caps how many distinct email addresses a single source IP may fail to authenticate as
/// within a rolling window, independent of the per-IP total-request rate limiter.
///
/// The per-IP request limiter alone does not stop a targeted account-lockout denial of service:
/// with a generous per-IP budget, a single attacker can spread exactly enough wrong-password
/// attempts across several known email addresses to trip Identity's own lockout on each of them,
/// then repeat the spray every time the lockouts expire. This guard makes that spread itself the
/// throttled resource. Only genuine authentication failures count against the cap - a source that
/// legitimately logs in as many different real accounts (a shared office IP, a test suite, a
/// monitoring account) is never penalized, since successes are never recorded here.
/// </summary>
public interface ILoginAttemptGuard
{
    /// <summary>Call before attempting authentication. True means reject with 429 immediately.</summary>
    bool ShouldThrottle(string sourceIp, string email);

    /// <summary>Call only after a genuine authentication failure (never after a success).</summary>
    void RecordFailedAttempt(string sourceIp, string email);
}

public sealed class LoginAttemptGuard(IMemoryCache cache) : ILoginAttemptGuard
{
    private const int MaxDistinctFailedTargetsPerWindow = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public bool ShouldThrottle(string sourceIp, string email)
    {
        var targets = GetTargets(sourceIp);
        lock (targets)
        {
            return !targets.Contains(email) && targets.Count >= MaxDistinctFailedTargetsPerWindow;
        }
    }

    public void RecordFailedAttempt(string sourceIp, string email)
    {
        var targets = GetTargets(sourceIp);
        lock (targets)
        {
            if (targets.Count < MaxDistinctFailedTargetsPerWindow)
            {
                targets.Add(email);
            }
        }
    }

    private HashSet<string> GetTargets(string sourceIp) =>
        cache.GetOrCreate($"login-attempt-guard:{sourceIp}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Window;
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        })!;
}
