namespace TriloBot.RemoteController;

/// <summary>
/// Represents the current state of an Xbox controller with all input values.
/// Used to track normalized axis positions and button states.
/// </summary>
public class ControllerState
{
    /// <summary>
    /// Normalized horizontal position of the left analog stick (-1.0 to 1.0).
    /// </summary>
    public double LeftStickX { get; set; } = 0.0;

    /// <summary>
    /// Normalized vertical position of the left analog stick (-1.0 to 1.0).
    /// </summary>
    public double LeftStickY { get; set; } = 0.0;

    /// <summary>
    /// Normalized position of the left trigger (0.0 to 1.0).
    /// </summary>
    public double LeftTrigger { get; set; } = 0.0;

    /// <summary>
    /// Normalized position of the right trigger (0.0 to 1.0).
    /// </summary>
    public double RightTrigger { get; set; } = 0.0;

    /// <summary>
    /// Current state of the A button (face button).
    /// </summary>
    public bool AButton { get; set; }

    /// <summary>
    /// Current state of the B button (face button).
    /// </summary>
    public bool BButton { get; set; }

    /// <summary>
    /// Current state of the X button (face button).
    /// </summary>
    public bool XButton { get; set; }

    /// <summary>
    /// Current state of the Y button (face button).
    /// </summary>
    public bool YButton { get; set; }

    /// <summary>
    /// Resets all controller state values to their default (neutral) positions.
    /// </summary>
    public void Reset()
    {
        LeftStickX = 0.0;
        LeftStickY = 0.0;
        LeftTrigger = 0.0;
        RightTrigger = 0.0;
        AButton = false;
        BButton = false;
        XButton = false;
        YButton = false;
    }
}
