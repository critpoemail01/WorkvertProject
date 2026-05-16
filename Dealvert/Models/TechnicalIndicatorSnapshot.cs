namespace Dealvert.Models;

public record TechnicalIndicatorSnapshot(
    string Symbol,
    string Timeframe,
    decimal Rsi,
    decimal FastEma,
    decimal SlowEma,
    decimal? PreviousFastEma,
    decimal? PreviousSlowEma,
    DateTime AsOfUtc
);
