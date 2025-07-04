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

    #region Underlights

    /// <summary>Index for the front right underlight.</summary>
    internal const int LightFrontRight = 0;
   
    /// <summary>Index for the front left underlight.</summary>
    internal const int LightFrontLeft = 1;
   
    /// <summary>Index for the middle left underlight.</summary>
    internal const int LightMiddleLeft = 2;
   
    /// <summary>Index for the rear left underlight.</summary>
    internal const int LightRearLeft = 3;
   
    /// <summary>Index for the rear right underlight.</summary>
    internal const int LightRearRight = 4;
   
    /// <summary>Index for the middle right underlight.</summary>
    internal const int LightMiddleRight = 5;
   
    /// <summary>Total number of underlights.</summary>
    internal const int NumberOfLights = 6;

    /// <summary>Indices for all left-side underlights.</summary>
    internal static readonly int[] LightsLeft = [LightFrontLeft, LightMiddleLeft, LightRearLeft];
   
    /// <summary>Indices for all right-side underlights.</summary>
    internal static readonly int[] LightsRight = [LightFrontRight, LightMiddleRight, LightRearRight];
   
    /// <summary>Indices for all front underlights.</summary>
    internal static readonly int[] LightsFront = [LightFrontLeft, LightFrontRight];
   
    /// <summary>Indices for all middle underlights.</summary>
    internal static readonly int[] LightsMiddle = [LightMiddleLeft, LightMiddleRight];

    /// <summary>Indices for all rear underlights.</summary>
    internal static readonly int[] LightsRear = [LightRearLeft, LightRearRight];

    /// <summary>Indices for left diagonal underlights.</summary>
    internal static readonly int[] LightsLeftDiagonal = [LightFrontLeft, LightRearRight];

    /// <summary>Indices for right diagonal underlights.</summary>
    internal static readonly int[] LightsRightDiagonal = [LightFrontRight, LightRearLeft];

    #endregion
}
