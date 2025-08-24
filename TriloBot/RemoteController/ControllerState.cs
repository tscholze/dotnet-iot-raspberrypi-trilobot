namespace TriloBot.RemoteController;

/// <summary>
/// Represents the current state of an Xbox controller.
/// </summary>
public class ControllerState
{
    /// <summary>
    /// Left stick X-axis value (-1.0 to 1.0) from Xbox controller.
    /// </summary>
    public double LeftStickX { get; set; }

    /// <summary>
    /// Left trigger value (0.0 to 1.0) from Xbox controller.
    /// </summary>
    public double LeftTrigger { get; set; }

    /// <summary>
    /// Right trigger value (0.0 to 1.0) from Xbox controller.
    /// </summary>
    public double RightTrigger { get; set; }

    /// <summary>
    /// State of the A button on Xbox controller.
    /// </summary>
    public bool AButton { get; set; }

    /// <summary>
    /// State of the B button on Xbox controller.
    /// </summary>
    public bool BButton { get; set; }

    /// <summary>
    /// State of the X button on Xbox controller.
    /// </summary>
    public bool XButton { get; set; }

    /// <summary>
    /// State of the Y button on Xbox controller.
    /// </summary>
    public bool YButton { get; set; }
}
