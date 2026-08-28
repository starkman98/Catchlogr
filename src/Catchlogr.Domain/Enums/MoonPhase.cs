namespace Catchlogr.Domain.Enums;

/// <summary>
/// Identifies one of the eight commonly presented phases of the Moon.
/// </summary>
public enum MoonPhase
{
    /// <summary>The Moon is near the start of its illumination cycle.</summary>
    NewMoon,

    /// <summary>The illuminated portion is increasing toward first quarter.</summary>
    WaxingCrescent,

    /// <summary>Approximately half of the visible lunar disk is illuminated and increasing.</summary>
    FirstQuarter,

    /// <summary>More than half of the visible lunar disk is illuminated and increasing.</summary>
    WaxingGibbous,

    /// <summary>The visible lunar disk is near maximum illumination.</summary>
    FullMoon,

    /// <summary>More than half of the visible lunar disk is illuminated and decreasing.</summary>
    WaningGibbous,

    /// <summary>Approximately half of the visible lunar disk is illuminated and decreasing.</summary>
    LastQuarter,

    /// <summary>The illuminated portion is decreasing toward new moon.</summary>
    WaningCrescent
}
