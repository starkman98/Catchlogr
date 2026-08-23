namespace FishingLog.Mobile.Presentation;

/// <summary>
/// Formats server-calculated moon phases for mobile presentation.
/// </summary>
internal static class MoonPhasePresentationFormatter
{
    /// <summary>Gets a lunar symbol for the supplied serialized phase.</summary>
    public static string GetIcon(string? moonPhase) => moonPhase switch
    {
        "NewMoon" => "🌑",
        "WaxingCrescent" => "🌒",
        "FirstQuarter" => "🌓",
        "WaxingGibbous" => "🌔",
        "FullMoon" => "🌕",
        "WaningGibbous" => "🌖",
        "LastQuarter" => "🌗",
        "WaningCrescent" => "🌘",
        _ => string.Empty
    };

    /// <summary>Gets a readable label for the supplied serialized phase.</summary>
    public static string GetDisplayName(string? moonPhase) => moonPhase switch
    {
        "NewMoon" => "New moon",
        "WaxingCrescent" => "Waxing crescent",
        "FirstQuarter" => "First quarter",
        "WaxingGibbous" => "Waxing gibbous",
        "FullMoon" => "Full moon",
        "WaningGibbous" => "Waning gibbous",
        "LastQuarter" => "Last quarter",
        "WaningCrescent" => "Waning crescent",
        _ => string.Empty
    };
}
