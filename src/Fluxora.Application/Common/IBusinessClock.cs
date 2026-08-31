namespace Fluxora.Application.Common;

public interface IBusinessClock
{
    DateOnly Today { get; }

    DateTime StartOfDayUtc(DateOnly localDate);
}
