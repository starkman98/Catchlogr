using Catchlogr.Domain.Enums;

namespace Catchlogr.Application.Interfaces;

/// <summary>
/// Calculates the commonly presented lunar phase for a UTC timestamp.
/// </summary>
public interface IMoonPhaseService
{
    /// <summary>
    /// Calculates the lunar phase at the supplied UTC timestamp.
    /// </summary>
    /// <param name="timestampUtc">The instant for which the phase is calculated.</param>
    /// <returns>The corresponding eight-part lunar phase.</returns>
    MoonPhase Calculate(DateTime timestampUtc);
}
