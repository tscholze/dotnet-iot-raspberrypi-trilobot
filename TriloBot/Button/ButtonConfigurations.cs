namespace TriloBot.Button;

/// <summary>
/// Provides configuration constants and groupings for TriloBot's buttons.
/// </summary>
public static class ButtonConfigurations
{
    /// <summary>GPIO pin for Button A.</summary>
    private const int ButtonAPin = 5;

    /// <summary>GPIO pin for Button B.</summary>
    private const int ButtonBPin = 6;

    /// <summary>GPIO pin for Button X.</summary>
    private const int ButtonXPin = 16;

    /// <summary>GPIO pin for Button Y.</summary>
    private const int ButtonYPin = 24;

    /// <summary>Array of all button GPIO pins.</summary>
    internal static readonly int[] ButtonPins = { ButtonAPin, ButtonBPin, ButtonXPin, ButtonYPin };

    /// <summary>Total number of buttons.</summary>
    internal const int NumberOfButtons = 4;

    /// <summary>Index for Button A.</summary>
    internal const int ButtonA = 0;

    /// <summary>Index for Button B.</summary>
    internal const int ButtonB = 1;

    /// <summary>Index for Button X.</summary>
    internal const int ButtonX = 2;

    /// <summary>Index for Button Y.</summary>
    internal const int ButtonY = 3;
}
