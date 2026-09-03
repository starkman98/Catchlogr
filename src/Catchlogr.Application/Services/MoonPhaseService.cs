using Catchlogr.Application.Interfaces;
using Catchlogr.Domain.Enums;

namespace Catchlogr.Application.Services;

/// <summary>
/// Calculates an eight-part lunar phase from a known new moon and the average
/// synodic-month duration.
/// </summary>
public sealed class MoonPhaseService : IMoonPhaseService
{
    private const double SynodicMonthDays = 29.530588853;

    private static readonly DateTime ReferenceNewMoonUtc =
        new(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public MoonPhase Calculate(DateTime timestampUtc)
    {
        if (timestampUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Moon phase timestamps must use DateTimeKind.Utc.",
                nameof(timestampUtc));
        }

        var elapsedDays = (timestampUtc - ReferenceNewMoonUtc).TotalDays;
        var lunarAgeDays = elapsedDays % SynodicMonthDays;
        if (lunarAgeDays < 0)
            lunarAgeDays += SynodicMonthDays;

        var cycleFraction = lunarAgeDays / SynodicMonthDays;
        var phaseIndex = (int)Math.Floor((cycleFraction * 8) + 0.5) % 8;

        return phaseIndex switch
        {
            0 => MoonPhase.NewMoon,
            1 => MoonPhase.WaxingCrescent,
            2 => MoonPhase.FirstQuarter,
            3 => MoonPhase.WaxingGibbous,
            4 => MoonPhase.FullMoon,
            5 => MoonPhase.WaningGibbous,
            6 => MoonPhase.LastQuarter,
            7 => MoonPhase.WaningCrescent,
            _ => throw new InvalidOperationException("The calculated moon phase index is invalid.")
        };
    }
}
