using System.Collections.Concurrent;

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
///
/// The cap check and the reservation of a new distinct target happen atomically under the same
/// per-source-IP lock (see <see cref="Reserve"/>): concurrent requests for several new targets from
/// the same source cannot all observe room under the cap and all be admitted, because the first one
/// to acquire the lock immediately claims its slot before releasing it. This requires the bucket
/// itself - not just access to it - to be shared by every concurrent caller for that source IP;
/// <see cref="ConcurrentDictionary{TKey,TValue}.AddOrUpdate"/> guarantees all callers converge on
/// the single value that actually ends up stored, even if multiple threads race to create it for a
/// brand-new key. (A prior version of this class used IMemoryCache.GetOrCreate for that step, whose
/// factory delegate is not atomic under concurrent first access: racing callers could each receive
/// their own distinct HashSet instance and lock on it independently, defeating the cap entirely.)
/// </summary>
public interface ILoginAttemptGuard
{
    /// <summary>
    /// Call before attempting authentication. Atomically checks the distinct-target cap and, if
    /// admitted, provisionally reserves the target so a concurrent request against a different new
    /// target from the same source is correctly throttled too. The caller must resolve the lease:
    /// <see cref="ILoginAttemptLease.ConfirmFailure"/> after a genuine authentication failure, or
    /// <see cref="ILoginAttemptLease.Release"/> after anything else (success, unrelated error), so a
    /// target that never actually failed is never left counted against the cap.
    /// </summary>
    ILoginAttemptLease Reserve(string sourceIp, string email);
}

public interface ILoginAttemptLease
{
    /// <summary>True means reject immediately with 429; do not attempt authentication.</summary>
    bool Throttled { get; }

    /// <summary>Call only after a genuine authentication failure for this target.</summary>
    void ConfirmFailure();

    /// <summary>Call for any outcome other than a genuine authentication failure.</summary>
    void Release();
}

public sealed class LoginAttemptGuard : ILoginAttemptGuard
{
    private const int MaxDistinctFailedTargetsPerWindow = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, Bucket> _buckets = new();

    public ILoginAttemptLease Reserve(string sourceIp, string email)
    {
        var now = DateTimeOffset.UtcNow;
        var bucket = _buckets.AddOrUpdate(
            sourceIp,
            static (_, w) => new Bucket(w.now + w.Window),
            static (_, existing, w) => existing.ExpiresAt > w.now ? existing : new Bucket(w.now + w.Window),
            (now, Window));

        lock (bucket.Targets)
        {
            if (bucket.Targets.Contains(email))
            {
                return new Lease(bucket.Targets, email, throttled: false, provisional: false);
            }

            if (bucket.Targets.Count >= MaxDistinctFailedTargetsPerWindow)
            {
                return new Lease(bucket.Targets, email, throttled: true, provisional: false);
            }

            bucket.Targets.Add(email);
            return new Lease(bucket.Targets, email, throttled: false, provisional: true);
        }
    }

    private sealed class Bucket(DateTimeOffset expiresAt)
    {
        public DateTimeOffset ExpiresAt { get; } = expiresAt;
        public HashSet<string> Targets { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class Lease(HashSet<string> targets, string email, bool throttled, bool provisional) : ILoginAttemptLease
    {
        public bool Throttled { get; } = throttled;

        public void ConfirmFailure()
        {
            // Already claimed eagerly in Reserve; nothing further to record.
        }

        public void Release()
        {
            if (!provisional)
            {
                return;
            }

            lock (targets)
            {
                targets.Remove(email);
            }
        }
    }
}
