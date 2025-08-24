namespace TriloBot.RemoteController;

/// <summary>
/// Configuration settings for controller input processing.
/// </summary>
public static class ControllerConfiguration
{
    /// <summary>
    /// Minimum change threshold for movement values to reduce noise and prevent excessive updates.
    /// </summary>
    public const double MovementThreshold = 0.1;

    /// <summary>
    /// Polling interval for controller input monitoring in milliseconds.
    /// </summary>
    public const int PollingIntervalMs = 50;

    /// <summary>
    /// Dead zone radius for left stick to prevent drift and unintended movement.
    /// </summary>
    public const double StickDeadZone = 0.15;

    /// <summary>
    /// Dead zone threshold for triggers to ensure clean zero state.
    /// </summary>
    public const double TriggerDeadZone = 0.05;

    /// <summary>
    /// Maximum wait time for monitoring task shutdown in seconds.
    /// </summary>
    public const int ShutdownTimeoutSeconds = 2;

    /// <summary>
    /// Size of Linux input_event structure in bytes.
    /// </summary>
    public const int InputEventSize = 24;
}
