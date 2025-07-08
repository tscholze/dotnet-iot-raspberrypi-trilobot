namespace TriloBot.Light;

/// <summary>
/// Enum representing the underlight positions on the TriloBot.
/// </summary>
public enum Lights
{
    /// <summary>Front right underlight (index 0).</summary>
    LIGHT_FRONT_RIGHT = 0,

    /// <summary>Front left underlight (index 1).</summary>
    LIGHT_FRONT_LEFT = 1,

    /// <summary>Middle left underlight (index 2).</summary>
    LIGHT_MIDDLE_LEFT = 2,

    /// <summary>Rear left underlight (index 3).</summary>
    LIGHT_REAR_LEFT = 3,

    /// <summary>Rear right underlight (index 4).</summary>
    LIGHT_REAR_RIGHT = 4,

    /// <summary>Middle right underlight (index 5).</summary>
    LIGHT_MIDDLE_RIGHT = 5,

    LIGHT_LED_A = 6,
    LIGHT_LED_B = 7,
    LIGHT_LED_X = 8,
    LIGHT_LED_Y = 9
}

/// <summary>
/// Extension methods and groupings for the <see cref="Lights"/> enum.
/// </summary>
public static class LightsExtensions
{
    /// <summary>
    /// Returns the GPIO pin number for the button LED corresponding to the underlight position.
    /// </summary>
    /// <param name="light">The <see cref="Lights"/> enum value.</param>
    /// <returns>The GPIO pin number for the corresponding LED.</returns>
    public static int ToPinNumber(this Lights light)
    {
        return light switch
        {
            Lights.LIGHT_LED_A => 23, // LedAPin
            Lights.LIGHT_LED_B => 22,  // LedBPin
            Lights.LIGHT_LED_X => 17, // LedXPin
            Lights.LIGHT_LED_Y => 27,   // LedYPin
            _ => throw new ArgumentOutOfRangeException(nameof(light), light, null)
        };
    }

    /// <summary>
    /// Returns a localized name for the underlight position.
    /// </summary>
    /// <param name="light">The <see cref="Lights"/> enum value.</param>
    /// <returns>A human-readable name for the underlight position.</returns>
    public static string ToLocalizedName(this Lights light)
    {
        return light switch
        {
            Lights.LIGHT_FRONT_RIGHT => "Front Right",
            Lights.LIGHT_FRONT_LEFT => "Front Left",
            Lights.LIGHT_MIDDLE_LEFT => "Middle Left",
            Lights.LIGHT_REAR_LEFT => "Rear Left",
            Lights.LIGHT_REAR_RIGHT => "Rear Right",
            Lights.LIGHT_MIDDLE_RIGHT => "Middle Right",
            _ => light.ToString()
        };
    }

    /// <summary>
    /// Gets all left-side underlights.
    /// </summary>
    public static Lights[] LightsLeft => new[]
    {
        Lights.LIGHT_FRONT_LEFT, Lights.LIGHT_MIDDLE_LEFT, Lights.LIGHT_REAR_LEFT
    };

    /// <summary>
    /// Gets all right-side underlights.
    /// </summary>
    public static Lights[] LightsRight => new[]
    {
        Lights.LIGHT_FRONT_RIGHT, Lights.LIGHT_MIDDLE_RIGHT, Lights.LIGHT_REAR_RIGHT
    };

    /// <summary>
    /// Gets all front underlights.
    /// </summary>
    public static Lights[] LightsFront => new[]
    {
        Lights.LIGHT_FRONT_LEFT, Lights.LIGHT_FRONT_RIGHT
    };

    /// <summary>
    /// Gets all middle underlights.
    /// </summary>
    public static Lights[] LightsMiddle => new[]
    {
        Lights.LIGHT_MIDDLE_LEFT, Lights.LIGHT_MIDDLE_RIGHT
    };

    /// <summary>
    /// Gets all rear underlights.
    /// </summary>
    public static Lights[] LightsRear => new[]
    {
        Lights.LIGHT_REAR_LEFT, Lights.LIGHT_REAR_RIGHT
    };

    /// <summary>
    /// Gets the left diagonal underlights (front left and rear right).
    /// </summary>
    public static Lights[] LightsLeftDiagonal => new[]
    {
        Lights.LIGHT_FRONT_LEFT, Lights.LIGHT_REAR_RIGHT
    };

    /// <summary>
    /// Gets the right diagonal underlights (front right and rear left).
    /// </summary>
    public static Lights[] LightsRightDiagonal => new[]
    {
        Lights.LIGHT_FRONT_RIGHT, Lights.LIGHT_REAR_LEFT
    };
}
