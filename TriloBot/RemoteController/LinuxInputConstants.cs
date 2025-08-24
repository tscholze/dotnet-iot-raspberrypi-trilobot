namespace TriloBot.RemoteController;

/// <summary>
/// Linux input system constants for Xbox controller interaction.
/// </summary>
public static class LinuxInputConstants
{
    /// <summary>
    /// Linux input event types (subset).
    /// </summary>
    public enum EventType : ushort
    {
        /// <summary>
        /// Button press/release events (EV_KEY). Used for digital input like A, B, X, Y buttons.
        /// Value 0 indicates button release, value 1 indicates button press.
        /// </summary>
        Key = 0x01,
        
        /// <summary>
        /// Absolute axis events (EV_ABS). Used for analog input like sticks, triggers, and D-pad.
        /// Values represent position within the axis range (e.g., -32767 to 32767 for sticks).
        /// </summary>
        Abs = 0x03
    }

    /// <summary>
    /// Linux ABS axis codes used by Xbox controllers.
    /// </summary>
    public enum AbsCode : ushort
    {
        /// <summary>
        /// Left analog stick horizontal axis (ABS_X). 
        /// Range: -32767 (full left) to 32767 (full right), with 0 as center.
        /// Used by both Xbox 360 and Xbox Series controllers.
        /// </summary>
        X = 0,
        
        /// <summary>
        /// Left trigger axis for Xbox 360 controllers (ABS_Z).
        /// Range: 0 (not pressed) to 255 (fully pressed).
        /// Only used by Xbox 360 controllers via USB connection.
        /// </summary>
        Z = 2,
        
        /// <summary>
        /// Right trigger axis for Xbox 360 controllers (ABS_RZ).
        /// Range: 0 (not pressed) to 255 (fully pressed).
        /// Only used by Xbox 360 controllers via USB connection.
        /// </summary>
        RZ = 5,
        
        /// <summary>
        /// Right trigger axis for Xbox Series controllers via Bluetooth (ABS_GAS).
        /// Range: 0 (not pressed) to 1023 (fully pressed).
        /// Used by Xbox Series/One controllers when connected via Bluetooth.
        /// </summary>
        GAS = 9,
        
        /// <summary>
        /// Left trigger axis for Xbox Series controllers via Bluetooth (ABS_BRAKE).
        /// Range: 0 (not pressed) to 1023 (fully pressed).
        /// Used by Xbox Series/One controllers when connected via Bluetooth.
        /// </summary>
        BRAKE = 10
    }

    /// <summary>
    /// Linux button codes used by Xbox controllers.
    /// </summary>
    public enum BtnCode : ushort
    {
        /// <summary>
        /// A button (bottom face button) - BTN_A.
        /// Linux input code 304. Typically used for primary action/confirmation.
        /// Maps to TriloBot.Button.Buttons.ButtonA.
        /// </summary>
        A = 304,
        
        /// <summary>
        /// B button (right face button) - BTN_B.
        /// Linux input code 305. Typically used for secondary action/cancellation.
        /// Maps to TriloBot.Button.Buttons.ButtonB.
        /// </summary>
        B = 305,
        
        /// <summary>
        /// X button (left face button) - BTN_X.
        /// Linux input code 307. Typically used for tertiary actions.
        /// Maps to TriloBot.Button.Buttons.ButtonX.
        /// </summary>
        X = 307,
        
        /// <summary>
        /// Y button (top face button) - BTN_Y.
        /// Linux input code 308. Typically used for quaternary actions.
        /// Maps to TriloBot.Button.Buttons.ButtonY.
        /// </summary>
        Y = 308
    }

    /// <summary>
    /// Xbox controller vendor/product IDs for hardware identification.
    /// </summary>
    public static class HardwareIds
    {
        /// <summary>
        /// Microsoft Corporation USB vendor ID.
        /// Used to identify Microsoft-manufactured Xbox controllers in the Linux input subsystem.
        /// </summary>
        public const string MicrosoftVendorId = "045e";
        
        /// <summary>
        /// Xbox 360 wired controller USB product ID.
        /// Combined with Microsoft vendor ID (045e:028e) to identify wired Xbox 360 controllers.
        /// </summary>
        public const string Xbox360WiredProductId = "028e";
        
        /// <summary>
        /// Xbox 360 wireless controller USB product ID.
        /// Combined with Microsoft vendor ID (045e:028f) to identify wireless Xbox 360 controllers
        /// connected via the Xbox 360 Wireless Gaming Receiver.
        /// </summary>
        public const string Xbox360WirelessProductId = "028f";
    }

    /// <summary>
    /// Common Xbox controller device name patterns for identification.
    /// These patterns are used to match against device names in /sys/class/input/eventN/device/name
    /// for Xbox controller detection when hardware ID matching is not available or reliable.
    /// </summary>
    public static readonly string[] XboxDevicePatterns = 
    [
        "xbox", "xbox 360", "xbox one", "xbox series", 
        "xbox wireless controller", "xbox360", "gamepad"
    ];
}
