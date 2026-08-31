using Fluxora.Application.Common;

namespace Fluxora.Infrastructure.Time;

public sealed class BusinessClock(TimeProvider timeProvider, string timeZoneId) : IBusinessClock
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    public DateOnly Today => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _timeZone).DateTime);

    public DateTime StartOfDayUtc(DateOnly localDate)
    {
        var localStart = localDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        if (_timeZone.IsInvalidTime(localStart))
        {
            localStart = localStart.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(localStart, _timeZone);
    }
}
