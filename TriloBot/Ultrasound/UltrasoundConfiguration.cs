namespace TriloBot.Ultrasound;

/// <summary>
/// Configuration settings for the ultrasound sensor.
/// </summary>
internal static class UltrasoundConfiguration
{
    /// <summary>
    /// GPIO pin number for the trigger signal of the ultrasound sensor.
    /// </summary>
    internal const int UltraTrigPin = 13;

    /// <summary>
    /// GPIO pin number for the echo signal of the ultrasound sensor.
    /// </summary>
    internal const int UltraEchoPin = 25;

    /// <summary>
    /// Speed of sound in centimeters per nanosecond.
    /// </summary>
    internal const double SpeedOfSoundCmNs = 343 * 100.0 / 1E9; // 0.0000343 cm/ns

    /// <summary>
    /// Timeout in milliseconds for the ultrasound sensor to receive an echo.
    /// </summary>
    internal const int Timeout = 50;

    /// <summary>
    /// Number of samples to take for averaging distance measurements.
    /// </summary>
    internal const int Samples = 3;
}
