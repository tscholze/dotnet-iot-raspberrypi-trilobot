using System.Reactive.Linq;
using System.Reactive.Subjects;
using TriloBot.Button;
using System.IO;
using System.Runtime.InteropServices;

namespace TriloBot.RemoteController;

/// <summary>
/// Manages Xbox 360 controller input for robot control using wired connection.
/// Right trigger controls vertical movement, left stick controls horizontal movement.
/// </summary>
public sealed class RemoteControllerManager : IDisposable
{
    #region Public Properties

    /// <summary>
    /// Observable that emits horizontal movement values from the left stick (-1.0 to 1.0).
    /// </summary>
    public IObservable<double> HorizontalMovementObservable => _horizontalMovementSubject.AsObservable();

    /// <summary>
    /// Observable that emits vertical movement values from the right trigger (0.0 to 1.0).
    /// </summary>
    public IObservable<double> VerticalMovementObservable => _verticalMovementSubject.AsObservable();

    /// <summary>
    /// Observable that emits button press events for mapped controller buttons.
    /// </summary>
    public IObservable<Buttons?> ButtonPressedObservable => _buttonPressedSubject.AsObservable();

    /// <summary>
    /// Indicates whether a controller is currently connected.
    /// </summary>
    public bool IsControllerConnected => _isControllerConnected;

    #endregion

    #region Private Fields

    private readonly BehaviorSubject<double> _horizontalMovementSubject = new(0.0);
    private readonly BehaviorSubject<double> _verticalMovementSubject = new(0.0);
    private readonly BehaviorSubject<Buttons?> _buttonPressedSubject = new(null);
    
    private Task? _controllerMonitoringTask;
    private CancellationTokenSource? _controllerMonitoringCts;
    private bool _isControllerConnected;
    private bool _disposed;

    // Controller input file streams
    private FileStream? _controllerInputStream;
    private string? _controllerDevicePath;

    // Previous state tracking for button press detection
    private bool _previousAButton;
    private bool _previousBButton;
    private bool _previousXButton;
    private bool _previousYButton;
    private double _previousHorizontal;
    private double _previousVertical;

    // Current controller state
    private readonly ControllerState _currentState = new();

    #endregion

    #region Private Constants

    /// <summary>
    /// Minimum change threshold for movement values to reduce noise.
    /// </summary>
    private const double MovementThreshold = 0.05;

    /// <summary>
    /// Polling interval for controller input in milliseconds.
    /// </summary>
    private const int PollingIntervalMs = 50;

    /// <summary>
    /// Dead zone for left stick to prevent drift.
    /// </summary>
    private const double StickDeadZone = 0.15;

    /// <summary>
    /// Dead zone for trigger to ensure clean zero state.
    /// </summary>
    private const double TriggerDeadZone = 0.05;

    // Linux input event constants for Xbox 360 controller
    private const ushort EV_KEY = 0x01;
    private const ushort EV_ABS = 0x03;
    
    // Xbox 360 controller button codes
    private const ushort BTN_A = 304;      // A button
    private const ushort BTN_B = 305;      // B button  
    private const ushort BTN_X = 307;      // X button
    private const ushort BTN_Y = 308;      // Y button
    
    // Xbox 360 controller axis codes
    private const ushort ABS_X = 0;        // Left stick X-axis
    private const ushort ABS_RZ = 5;       // Right trigger (RT)

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the RemoteControllerManager class for Xbox 360 controller.
    /// </summary>
    public RemoteControllerManager()
    {
        // Xbox 360 controller initialization will be handled in StartMonitoring
        StartMonitoring();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Starts monitoring the Xbox 360 controller for input.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when controller monitoring is already active.</exception>
    public void StartMonitoring()
    {
        if (_controllerMonitoringTask is { IsCompleted: false })
            throw new InvalidOperationException("Xbox 360 controller monitoring is already active.");

        _controllerMonitoringCts = new CancellationTokenSource();
        _controllerMonitoringTask = Task.Run(async () =>
        {
            while (!_controllerMonitoringCts.Token.IsCancellationRequested)
            {
                try
                {
                    await PollControllerInput();
                    await Task.Delay(PollingIntervalMs, _controllerMonitoringCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Xbox 360 controller monitoring error: {ex.Message}");
                    _isControllerConnected = false;
                }
            }
        });
    }

    /// <summary>
    /// Stops monitoring the Xbox 360 controller.
    /// </summary>
    public void StopMonitoring()
    {
        if (_controllerMonitoringCts != null)
        {
            _controllerMonitoringCts.Cancel();
            _controllerMonitoringTask?.Wait(TimeSpan.FromSeconds(2));
            _controllerMonitoringCts.Dispose();
            _controllerMonitoringCts = null;
        }

        _isControllerConnected = false;
        _horizontalMovementSubject.OnNext(0.0);
        _verticalMovementSubject.OnNext(0.0);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Polls the controller for current input state and processes changes.
    /// </summary>
    private async Task PollControllerInput()
    {
        // Ensure controller is connected
        if (!EnsureControllerConnected())
        {
            _isControllerConnected = false;
            return;
        }

        _isControllerConnected = true;

        // Read and process input events
        await ReadControllerEvents();

        // Process horizontal movement (left stick X-axis)
        var horizontal = ApplyDeadZone(_currentState.LeftStickX, StickDeadZone);
        if (Math.Abs(horizontal - _previousHorizontal) > MovementThreshold)
        {
            _horizontalMovementSubject.OnNext(horizontal);
            _previousHorizontal = horizontal;
        }

        // Process vertical movement (right trigger)
        var vertical = ApplyDeadZone(_currentState.RightTrigger, TriggerDeadZone);
        if (Math.Abs(vertical - _previousVertical) > MovementThreshold)
        {
            _verticalMovementSubject.OnNext(vertical);
            _previousVertical = vertical;
        }

        // Process button presses
        ProcessButtonChanges(_currentState);
    }

    /// <summary>
    /// Processes button state changes and emits events for newly pressed buttons.
    /// </summary>
    /// <param name="state">Current controller state</param>
    private void ProcessButtonChanges(ControllerState state)
    {
        // Check A button (maps to Button A)
        if (state.AButton && !_previousAButton)
        {
            _buttonPressedSubject.OnNext(Buttons.ButtonA);
        }
        _previousAButton = state.AButton;

        // Check B button (maps to Button B)
        if (state.BButton && !_previousBButton)
        {
            _buttonPressedSubject.OnNext(Buttons.ButtonB);
        }
        _previousBButton = state.BButton;

        // Check X button (maps to Button X)
        if (state.XButton && !_previousXButton)
        {
            _buttonPressedSubject.OnNext(Buttons.ButtonX);
        }
        _previousXButton = state.XButton;

        // Check Y button (maps to Button Y)
        if (state.YButton && !_previousYButton)
        {
            _buttonPressedSubject.OnNext(Buttons.ButtonY);
        }
        _previousYButton = state.YButton;
    }

    /// <summary>
    /// Applies dead zone filtering to raw input values.
    /// </summary>
    /// <param name="value">Raw input value</param>
    /// <param name="deadZone">Dead zone threshold</param>
    /// <returns>Filtered value with dead zone applied</returns>
    private static double ApplyDeadZone(double value, double deadZone)
    {
        if (Math.Abs(value) < deadZone)
            return 0.0;

        // Scale the value to account for the dead zone
        var sign = Math.Sign(value);
        var scaledValue = (Math.Abs(value) - deadZone) / (1.0 - deadZone);
        return sign * Math.Min(scaledValue, 1.0);
    }

    /// <summary>
    /// Ensures that a controller is connected and ready for input.
    /// </summary>
    /// <returns>True if controller is connected, false otherwise</returns>
    private bool EnsureControllerConnected()
    {
        // If already connected, return true
        if (_controllerInputStream != null && _isControllerConnected)
        {
            return true;
        }

        // Try to find and connect to Xbox 360 controller
        _controllerDevicePath = FindXbox360Controller();
        if (string.IsNullOrEmpty(_controllerDevicePath))
        {
            return false;
        }

        try
        {
            // Open the controller input stream
            _controllerInputStream = new FileStream(_controllerDevicePath, FileMode.Open, FileAccess.Read);
            Console.WriteLine($"Connected to Xbox 360 controller: {_controllerDevicePath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to Xbox 360 controller: {ex.Message}");
            _controllerInputStream?.Dispose();
            _controllerInputStream = null;
            return false;
        }
    }

    /// <summary>
    /// Finds the Xbox 360 controller device path in the Linux input system.
    /// </summary>
    /// <returns>Device path or null if not found</returns>
    private static string? FindXbox360Controller()
    {
        try
        {
            // Look for Xbox 360 controller in /dev/input/event*
            var eventDevices = Directory.GetFiles("/dev/input", "event*");
            
            foreach (var device in eventDevices)
            {
                try
                {
                    // Check if this is an Xbox 360 controller by reading device name
                    var nameFile = $"/sys/class/input/{Path.GetFileName(device)}/device/name";
                    if (File.Exists(nameFile))
                    {
                        var deviceName = File.ReadAllText(nameFile).Trim().ToLowerInvariant();
                        
                        // Xbox 360 controller specific identifiers
                        if (deviceName.Contains("xbox 360") || 
                            deviceName.Contains("microsoft xbox 360") ||
                            deviceName.Contains("xbox360") ||
                            deviceName.Contains("gamepad"))
                        {
                            Console.WriteLine($"Found Xbox 360 controller: {deviceName}");
                            return device;
                        }
                    }
                    
                    // Alternative: Check by vendor/product ID for Xbox 360 controllers
                    var vendorFile = $"/sys/class/input/{Path.GetFileName(device)}/device/id/vendor";
                    var productFile = $"/sys/class/input/{Path.GetFileName(device)}/device/id/product";
                    
                    if (File.Exists(vendorFile) && File.Exists(productFile))
                    {
                        var vendorId = File.ReadAllText(vendorFile).Trim();
                        var productId = File.ReadAllText(productFile).Trim();
                        
                        // Microsoft vendor ID (045e) and Xbox 360 controller product IDs
                        if (vendorId.Equals("045e", StringComparison.OrdinalIgnoreCase) &&
                            (productId.Equals("028e", StringComparison.OrdinalIgnoreCase) || // Wired Xbox 360
                             productId.Equals("028f", StringComparison.OrdinalIgnoreCase)))   // Wireless Xbox 360
                        {
                            Console.WriteLine($"Found Xbox 360 controller by ID: vendor={vendorId}, product={productId}");
                            return device;
                        }
                    }
                }
                catch
                {
                    // Skip devices that can't be read
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching for Xbox 360 controller: {ex.Message}");
        }

        Console.WriteLine("Xbox 360 controller not found. Make sure it's connected via USB.");
        return null;
    }

    /// <summary>
    /// Reads input events from the controller and updates the current state.
    /// </summary>
    private async Task ReadControllerEvents()
    {
        if (_controllerInputStream == null) return;

        try
        {
            // Read input events (Linux input_event structure: time, type, code, value)
            var buffer = new byte[24]; // input_event is 24 bytes on 64-bit systems
            var bytesRead = await _controllerInputStream.ReadAsync(buffer, 0, buffer.Length);
            
            if (bytesRead == 24)
            {
                // Parse the input event
                var type = BitConverter.ToUInt16(buffer, 16);
                var code = BitConverter.ToUInt16(buffer, 18);
                var value = BitConverter.ToInt32(buffer, 20);

                ProcessInputEvent(type, code, value);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading controller input: {ex.Message}");
            // Disconnect on error
            _controllerInputStream?.Dispose();
            _controllerInputStream = null;
            _isControllerConnected = false;
        }
    }

    /// <summary>
    /// Processes a single input event and updates the controller state.
    /// </summary>
    /// <param name="type">Event type (EV_KEY, EV_ABS, etc.)</param>
    /// <param name="code">Event code (button/axis identifier)</param>
    /// <param name="value">Event value</param>
    private void ProcessInputEvent(ushort type, ushort code, int value)
    {
        switch (type)
        {
            case EV_KEY: // Button events
                ProcessButtonEvent(code, value != 0);
                break;
                
            case EV_ABS: // Axis events
                ProcessAxisEvent(code, value);
                break;
        }
    }

    /// <summary>
    /// Processes button press/release events.
    /// </summary>
    /// <param name="code">Button code</param>
    /// <param name="pressed">True if pressed, false if released</param>
    private void ProcessButtonEvent(ushort code, bool pressed)
    {
        switch (code)
        {
            case BTN_A:
                _currentState.AButton = pressed;
                break;
            case BTN_B:
                _currentState.BButton = pressed;
                break;
            case BTN_X:
                _currentState.XButton = pressed;
                break;
            case BTN_Y:
                _currentState.YButton = pressed;
                break;
        }
    }

    /// <summary>
    /// Processes analog stick and trigger events for Xbox 360 controller.
    /// </summary>
    /// <param name="code">Axis code</param>
    /// <param name="value">Raw axis value</param>
    private void ProcessAxisEvent(ushort code, int value)
    {
        switch (code)
        {
            case ABS_X: // Left stick X-axis
                // Xbox 360: Normalize from range (-32768 to 32767) to (-1.0 to 1.0)
                _currentState.LeftStickX = Math.Max(-1.0, Math.Min(1.0, value / 32767.0));
                break;
                
            case ABS_RZ: // Right trigger (RT)
                // Xbox 360: Normalize from range (0 to 255) to (0.0 to 1.0)
                _currentState.RightTrigger = Math.Max(0.0, Math.Min(1.0, value / 255.0));
                break;
        }
    }

    #endregion

    #region IDisposable Support

    /// <summary>
    /// Releases all resources used by the RemoteControllerManager.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        StopMonitoring();
        
        _controllerInputStream?.Dispose();
        _controllerInputStream = null;
        
        _horizontalMovementSubject.Dispose();
        _verticalMovementSubject.Dispose();
        _buttonPressedSubject.Dispose();

        _disposed = true;
    }

    #endregion
}

/// <summary>
/// Represents the current state of an Xbox 360 controller.
/// </summary>
internal class ControllerState
{
    /// <summary>
    /// Left stick X-axis value (-1.0 to 1.0) from Xbox 360 controller.
    /// </summary>
    public double LeftStickX { get; set; }

    /// <summary>
    /// Right trigger value (0.0 to 1.0) from Xbox 360 controller.
    /// </summary>
    public double RightTrigger { get; set; }

    /// <summary>
    /// State of the A button on Xbox 360 controller.
    /// </summary>
    public bool AButton { get; set; }

    /// <summary>
    /// State of the B button on Xbox 360 controller.
    /// </summary>
    public bool BButton { get; set; }

    /// <summary>
    /// State of the X button on Xbox 360 controller.
    /// </summary>
    public bool XButton { get; set; }

    /// <summary>
    /// State of the Y button on Xbox 360 controller.
    /// </summary>
    public bool YButton { get; set; }
}