namespace TriloBot.Motor;

/// <summary>
/// Enum representing the motors on the TriloBot.
/// </summary>
public enum Motor
{
    /// <summary>Left motor (index 0).</summary>
    MotorLeft = 0,
    
    /// <summary>Right motor (index 1).</summary>
    MotorRight = 1
}

/// <summary>
/// Extension methods for the <see cref="Motor"/> enum.
/// </summary>
public static class MotorExtensions
{
    /// <summary>Returns the enabled pin for the motors.</summary>
    public static int GetEnablePin() => 26;

    /// <summary>
    /// Returns the positive pin for the given motor.
    /// </summary>
    public static int GetPositivePin(this Motor motor) => motor switch
    {
        Motor.MotorLeft => 8,
        Motor.MotorRight => 10,
        _ => throw new ArgumentOutOfRangeException(nameof(motor), motor, null)
    };

    /// <summary>
    /// Returns the negative pin for the given motor.
    /// </summary>
    public static int GetNegativePin(this Motor motor) => motor switch
    {
        Motor.MotorLeft => 11,
        Motor.MotorRight => 9,
        _ => throw new ArgumentOutOfRangeException(nameof(motor), motor, null)
    };

    public static int CorrectionFactor(this Motor motor) => motor switch
    {
        Motor.MotorLeft => -1,
        Motor.MotorRight => 1,
        _ => throw new ArgumentOutOfRangeException(nameof(motor), motor, null)
    };
}
