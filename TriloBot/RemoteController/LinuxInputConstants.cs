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
        Key = 0x01,
        Abs = 0x03
    }

    /// <summary>
    /// Linux ABS axis codes used by Xbox controllers.
    /// </summary>
    public enum AbsCode : ushort
    {
        X = 0,      // Left stick X
        Z = 2,      // Left trigger (Xbox 360)
        RZ = 5,     // Right trigger (Xbox 360)
        GAS = 9,    // Right trigger (Xbox Series Bluetooth)
        BRAKE = 10  // Left trigger (Xbox Series Bluetooth)
    }

    /// <summary>
    /// Linux button codes used by Xbox controllers.
    /// </summary>
    public enum BtnCode : ushort
    {
        A = 304,
        B = 305,
        X = 307,
        Y = 308
    }

    /// <summary>
    /// Xbox controller vendor/product IDs for hardware identification.
    /// </summary>
    public static class HardwareIds
    {
        public const string MicrosoftVendorId = "045e";
        public const string Xbox360WiredProductId = "028e";
        public const string Xbox360WirelessProductId = "028f";
    }

    /// <summary>
    /// Common Xbox controller device name patterns for identification.
    /// </summary>
    public static readonly string[] XboxDevicePatterns = 
    {
        "xbox", "xbox 360", "xbox one", "xbox series", 
        "xbox wireless controller", "xbox360", "gamepad"
    };
}
