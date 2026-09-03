using Catchlogr.Application.Services;
using Catchlogr.Domain.Enums;
using FluentAssertions;

namespace Catchlogr.Tests.MoonPhases;

/// <summary>
/// Unit tests for <see cref="MoonPhaseService"/>.
/// </summary>
public class MoonPhaseServiceTests
{
    private const double SynodicMonthDays = 29.530588853;
    private static readonly DateTime ReferenceNewMoonUtc =
        new(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);

    private readonly MoonPhaseService _sut = new();

    /// <summary>Verifies each eighth of the lunar cycle maps to its expected phase.</summary>
    [Theory]
    [InlineData(0, MoonPhase.NewMoon)]
    [InlineData(1, MoonPhase.WaxingCrescent)]
    [InlineData(2, MoonPhase.FirstQuarter)]
    [InlineData(3, MoonPhase.WaxingGibbous)]
    [InlineData(4, MoonPhase.FullMoon)]
    [InlineData(5, MoonPhase.WaningGibbous)]
    [InlineData(6, MoonPhase.LastQuarter)]
    [InlineData(7, MoonPhase.WaningCrescent)]
    public void Calculate_CycleEighth_ReturnsExpectedPhase(
        int cycleEighth,
        MoonPhase expected)
    {
        var timestampUtc = ReferenceNewMoonUtc.AddDays(
            SynodicMonthDays * cycleEighth / 8);

        var result = _sut.Calculate(timestampUtc);

        result.Should().Be(expected);
    }

    /// <summary>Verifies negative modulo dates are normalized into the preceding cycle.</summary>
    [Fact]
    public void Calculate_DateBeforeReference_ReturnsPrecedingPhase()
    {
        var timestampUtc = ReferenceNewMoonUtc.AddDays(-SynodicMonthDays / 8);

        var result = _sut.Calculate(timestampUtc);

        result.Should().Be(MoonPhase.WaningCrescent);
    }

    /// <summary>Verifies timestamps without an explicit UTC kind are rejected.</summary>
    [Fact]
    public void Calculate_NonUtcTimestamp_ThrowsArgumentException()
    {
        var timestamp = DateTime.SpecifyKind(
            ReferenceNewMoonUtc,
            DateTimeKind.Unspecified);

        var action = () => _sut.Calculate(timestamp);

        action.Should().Throw<ArgumentException>();
    }
}
