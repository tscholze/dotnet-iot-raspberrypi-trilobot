namespace TriloBot.RemoteController;

/// <summary>
/// Specifies the type of Xbox controller for proper input handling and axis mapping.
/// Different controller types have different axis codes and value ranges.
/// </summary>
public enum ControllerType
{
    /// <summary>
    /// Xbox 360 controller (wired/wireless) - 8-bit triggers (0-255), standard axis mapping.
    /// </summary>
    Xbox360,
    
    /// <summary>
    /// Xbox Series/One controller (Bluetooth) - 10-bit triggers (0-1023), different axis codes.
    /// </summary>
    XboxSeries
}
