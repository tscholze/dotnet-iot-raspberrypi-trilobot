namespace TriloBot.RemoteController;

/// <summary>
/// Strategy interface for handling different Xbox controller types.
/// </summary>
public interface IControllerStrategy
{
    /// <summary>
    /// Processes axis events for the specific controller type.
    /// </summary>
    void ProcessAxisEvent(ushort code, int value, SharedControllerState state, ref int ltMax, ref int rtMax);
    
    /// <summary>
    /// Gets the initial trigger maximum values for this controller type.
    /// </summary>
    (int ltMax, int rtMax) GetInitialTriggerRanges();
}

/// <summary>
/// Strategy for Xbox 360 controller input processing.
/// </summary>
public class Xbox360Strategy : IControllerStrategy
{
    public void ProcessAxisEvent(ushort code, int value, SharedControllerState state, ref int ltMax, ref int rtMax)
    {
        switch ((LinuxInputConstants.AbsCode)code)
        {
            case LinuxInputConstants.AbsCode.X: // Left stick X-axis
                state.LeftStickX = Math.Clamp(value / 32767.0, -1.0, 1.0);
                break;
                
            case LinuxInputConstants.AbsCode.Z: // Left trigger (0-255)
                state.LeftTrigger = Math.Clamp(value / 255.0, 0.0, 1.0);
                break;
                
            case LinuxInputConstants.AbsCode.RZ: // Right trigger (0-255)
                state.RightTrigger = Math.Clamp(value / 255.0, 0.0, 1.0);
                break;
        }
    }

    public (int ltMax, int rtMax) GetInitialTriggerRanges() => (255, 255);
}

/// <summary>
/// Strategy for Xbox Series controller input processing.
/// </summary>
public class XboxSeriesStrategy : IControllerStrategy
{
    public void ProcessAxisEvent(ushort code, int value, SharedControllerState state, ref int ltMax, ref int rtMax)
    {
        switch (code)
        {
            case 0: // ABS_X - Left stick X-axis
                state.LeftStickX = Math.Clamp(value / 32767.0, -1.0, 1.0);
                break;
                
            case (ushort)LinuxInputConstants.AbsCode.BRAKE: // Left trigger (0-1023)
                state.LeftTrigger = Math.Clamp(value / 1023.0, 0.0, 1.0);
                break;
                
            case (ushort)LinuxInputConstants.AbsCode.GAS: // Right trigger (0-1023)
                state.RightTrigger = Math.Clamp(value / 1023.0, 0.0, 1.0);
                break;
                
            // Fallback handling for ABS_Z/ABS_RZ with adaptive scaling
            case (ushort)LinuxInputConstants.AbsCode.Z: // Fallback left trigger
                AdaptTriggerMax(ref ltMax, value);
                state.LeftTrigger = Math.Clamp(value / (double)ltMax, 0.0, 1.0);
                break;
                
            case (ushort)LinuxInputConstants.AbsCode.RZ: // Fallback right trigger
                AdaptTriggerMax(ref rtMax, value);
                state.RightTrigger = Math.Clamp(value / (double)rtMax, 0.0, 1.0);
                break;
        }
    }

    public (int ltMax, int rtMax) GetInitialTriggerRanges() => (1023, 1023);

    /// <summary>
    /// Adaptively updates the observed maximum trigger value for Xbox Series controllers.
    /// </summary>
    private static void AdaptTriggerMax(ref int currentMax, int value)
    {
        if (value > currentMax)
        {
            if (value > 4096 && currentMax < 65535)
            {
                currentMax = 65535; // High-resolution Bluetooth HID
            }
            else if (value > 255 && currentMax < 1023)
            {
                currentMax = 1023; // Common Bluetooth HID
            }
            else
            {
                currentMax = value; // Incremental growth for unknown variants
            }
        }
    }
}
