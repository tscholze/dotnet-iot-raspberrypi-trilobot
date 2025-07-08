namespace TriloBot.Button;

/// <summary>
/// Enum representing the four TriloBot buttons.
/// </summary>
public enum Buttons
{
    /// <summary>
    /// Button A (0).
    /// </summary>
    ButtonA = 0,

    /// <summary>
    /// Button B (1).
    /// </summary>
    ButtonB = 1,

    /// <summary>
    /// Button X (2).
    /// </summary>
    ButtonX = 2,

    /// <summary>
    /// Button Y (3).
    /// </summary>
    ButtonY = 3
}

#region Extensions

public static class ButtonsExtensions
{
    /// <summary>
    /// Returns a localized name for the button.
    /// </summary>
    public static string ToLocalizedName(this Buttons button)
    {
        return button switch
        {
            Buttons.ButtonA => "A",
            Buttons.ButtonB => "B",
            Buttons.ButtonX => "X",
            Buttons.ButtonY => "Y",
            _ => button.ToString()
        };
    }

    /// <summary>
    /// Returns the GPIO pin number for the button.
    /// </summary>
    public static int ToPinNumber(this Buttons button)
    {
        return button switch
        {
            Buttons.ButtonA => 5,
            Buttons.ButtonB => 6,
            Buttons.ButtonX => 16,
            Buttons.ButtonY => 24,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
        };
    }
}

#endregion