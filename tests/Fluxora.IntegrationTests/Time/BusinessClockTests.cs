using Fluxora.Infrastructure.Time;

namespace Fluxora.IntegrationTests.Time;

public class BusinessClockTests
{
    [Fact]
    public void Today_UsesConfiguredBusinessTimeZoneInsteadOfUtcDate()
    {
        var clock = new BusinessClock(
            new FixedTimeProvider(new DateTimeOffset(2026, 9, 1, 0, 30, 0, TimeSpan.Zero)),
            "America/Sao_Paulo");

        Assert.Equal(new DateOnly(2026, 8, 31), clock.Today);
    }

    [Fact]
    public void StartOfDayUtc_UsesBusinessTimeZoneOffset()
    {
        var clock = new BusinessClock(
            new FixedTimeProvider(DateTimeOffset.UtcNow),
            "America/Sao_Paulo");

        Assert.Equal(
            new DateTime(2026, 8, 31, 3, 0, 0, DateTimeKind.Utc),
            clock.StartOfDayUtc(new DateOnly(2026, 8, 31)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
