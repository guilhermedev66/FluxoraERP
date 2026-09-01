namespace Fluxora.Application.Common;

public interface IBusinessClock
{
    string TimeZoneId { get; }

    DateOnly Today { get; }

    DateTime StartOfDayUtc(DateOnly localDate);
}
