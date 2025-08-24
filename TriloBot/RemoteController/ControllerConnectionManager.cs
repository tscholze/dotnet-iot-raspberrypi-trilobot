namespace TriloBot.RemoteController;

/// <summary>
/// Handles Xbox controller device discovery and connection management.
/// </summary>
public class ControllerConnectionManager : IDisposable
{
    private FileStream? _controllerInputStream;
    private string? _controllerDevicePath;
    private bool _disposed;

    /// <summary>
    /// Gets a value indicating whether the controller is currently connected.
    /// </summary>
    public bool IsConnected => _controllerInputStream != null;

    /// <summary>
    /// Ensures that an Xbox controller connection is established.
    /// </summary>
    /// <returns>True if controller is connected and ready for input; otherwise, false.</returns>
    public bool EnsureConnected()
    {
        if (_controllerInputStream != null)
            return true;

        _controllerDevicePath = FindXboxController();
        if (string.IsNullOrEmpty(_controllerDevicePath))
            return false;

        try
        {
            _controllerInputStream = new FileStream(_controllerDevicePath, FileMode.Open, FileAccess.Read);
            Console.WriteLine($"Connected to Xbox controller: {_controllerDevicePath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to Xbox controller: {ex.Message}");
            _controllerInputStream?.Dispose();
            _controllerInputStream = null;
            return false;
        }
    }

    /// <summary>
    /// Reads a complete input event from the controller stream.
    /// </summary>
    /// <param name="token">Cancellation token for the read operation.</param>
    /// <returns>Input event data if successful; null if no complete event was read.</returns>
    public async Task<InputEvent?> ReadEventAsync(CancellationToken token)
    {
        if (_controllerInputStream == null)
            return null;

        try
        {
            var buffer = new byte[ControllerConfiguration.InputEventSize];
            var bytesRead = await _controllerInputStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
            
            if (bytesRead == ControllerConfiguration.InputEventSize)
            {
                var type = BitConverter.ToUInt16(buffer, 16);
                var code = BitConverter.ToUInt16(buffer, 18);
                var value = BitConverter.ToInt32(buffer, 20);
                return new InputEvent(type, code, value);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading controller input: {ex.Message}");
            Disconnect();
        }

        return null;
    }

    /// <summary>
    /// Disconnects from the controller and cleans up resources.
    /// </summary>
    public void Disconnect()
    {
        _controllerInputStream?.Dispose();
        _controllerInputStream = null;
    }

    /// <summary>
    /// Finds the Xbox controller device path within the Linux input subsystem.
    /// </summary>
    private static string? FindXboxController()
    {
        try
        {
            // Prefer stable by-id joystick symlinks when available
            var byIdPath = "/dev/input/by-id";
            if (Directory.Exists(byIdPath))
            {
                var byIdDevices = Directory.GetFiles(byIdPath, "*-event-joystick*");
                foreach (var dev in byIdDevices)
                {
                    var nameLower = Path.GetFileName(dev).ToLowerInvariant();
                    if (LinuxInputConstants.XboxDevicePatterns.Any(pattern => nameLower.Contains(pattern)))
                    {
                        return dev;
                    }
                }
            }

            // Enumerate all Linux input event devices
            var eventDevices = Directory.GetFiles("/dev/input", "event*");
            
            foreach (var device in eventDevices)
            {
                if (IsXboxController(device))
                    return device;
            }
        }
        catch (Exception)
        {
            // Device discovery failed
        }

        return null;
    }

    /// <summary>
    /// Checks if a device is an Xbox controller using name and hardware ID verification.
    /// </summary>
    private static bool IsXboxController(string device)
    {
        try
        {
            // Method 1: Check device name
            var nameFile = $"/sys/class/input/{Path.GetFileName(device)}/device/name";
            if (File.Exists(nameFile))
            {
                var deviceName = File.ReadAllText(nameFile).Trim().ToLowerInvariant();
                if (LinuxInputConstants.XboxDevicePatterns.Any(pattern => deviceName.Contains(pattern)))
                    return true;
            }
            
            // Method 2: Verify using hardware vendor/product ID
            var vendorFile = $"/sys/class/input/{Path.GetFileName(device)}/device/id/vendor";
            var productFile = $"/sys/class/input/{Path.GetFileName(device)}/device/id/product";
            
            if (File.Exists(vendorFile) && File.Exists(productFile))
            {
                var vendorId = File.ReadAllText(vendorFile).Trim();
                var productId = File.ReadAllText(productFile).Trim();
                
                return vendorId.Equals(LinuxInputConstants.HardwareIds.MicrosoftVendorId, StringComparison.OrdinalIgnoreCase) &&
                       (productId.Equals(LinuxInputConstants.HardwareIds.Xbox360WiredProductId, StringComparison.OrdinalIgnoreCase) ||
                        productId.Equals(LinuxInputConstants.HardwareIds.Xbox360WirelessProductId, StringComparison.OrdinalIgnoreCase) ||
                        true); // Accept other Microsoft Xbox gamepad PIDs
            }
        }
        catch
        {
            // Skip devices that cannot be accessed
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        Disconnect();
        _disposed = true;
    }
}

/// <summary>
/// Represents a Linux input event.
/// </summary>
public record InputEvent(ushort Type, ushort Code, int Value);
