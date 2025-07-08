namespace TriloBot.Light;

/// <summary>
/// Provides configuration constants and groupings for TriloBot's LEDs and underlights.
/// </summary>
internal static class LightsConfigurations
{
    #region Button LEDs

    /// <summary>GPIO pin for LED A.</summary>
    internal const int LedAPin = 23;

    /// <summary>GPIO pin for LED B.</summary>
    internal const int LedBPin = 22;

    /// <summary>GPIO pin for LED X.</summary>
    internal const int LedXPin = 17;
   
    /// <summary>GPIO pin for LED Y.</summary>
    internal const int LedYPin = 27;

    /// <summary>Array of all LED GPIO pins.</summary>
    internal static readonly int[] LedPins = [LedAPin, LedBPin, LedXPin, LedYPin];

    #endregion


    #endregion
}
