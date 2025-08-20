using System.Reactive.Linq;
using System.Reactive.Subjects;
using TriloBot.Button;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace TriloBot.RemoteController;

/// <summary>
/// Manages Xbox 360 controller input for robot control using wired USB connection.
/// Implements low-level Linux input event processing to provide real-time controller data.
/// Right trigger controls forward movement, left trigger controls backward movement, left stick controls horizontal movement.
/// </summary>
/// <remarks>
/// This class interfaces directly with the Linux input subsystem (/dev/input/event*) to read Xbox 360 controller events.
/// It processes raw input events and converts them to normalized values suitable for robot movement control.
/// The class uses reactive programming patterns with observables to emit movement and button events.
/// </remarks>
public sealed class RemoteControllerManager : IDisposable
{
    #region Public Properties

    /// <summary>
    /// Observable that emits horizontal movement values from the left stick X-axis.
    /// </summary>
    /// <value>
    /// Stream of normalized horizontal movement values in range (-1.0 to 1.0).
    /// Negative values indicate leftward movement, positive values indicate rightward movement.
    /// </value>
    /// <remarks>
    /// Values are filtered through dead zone processing and movement threshold detection to reduce noise.
    /// </remarks>
    public IObservable<double> HorizontalMovementObservable => _horizontalMovementSubject.AsObservable();

    /// <summary>
    /// Observable that emits vertical movement values from combined trigger inputs.
    /// </summary>
    /// <value>
    /// Stream of normalized vertical movement values in range (-1.0 to 1.0).
    /// Right trigger produces positive values (forward), left trigger produces negative values (backward).
    /// </value>
    /// <remarks>
    /// Calculated as: rightTrigger - leftTrigger, allowing for bidirectional control.
    /// Values are filtered through dead zone processing and movement threshold detection.
    /// </remarks>
    public IObservable<double> VerticalMovementObservable => _verticalMovementSubject.AsObservable();

    /// <summary>
    /// Observable that emits button press events for Xbox 360 controller action buttons.
    /// </summary>
    /// <value>
    /// Stream of button press events mapped to TriloBot button enumeration values.
    /// Emits null when no button is currently pressed.
    /// </value>
    /// <remarks>
    /// Only emits on button press (not release) to prevent duplicate events.
    /// Maps Xbox 360 A/B/X/Y buttons to corresponding TriloBot button values.
    /// </remarks>
    public IObservable<Buttons?> ButtonPressedObservable => _buttonPressedSubject.AsObservable();

    /// <summary>
    /// Gets a value indicating whether an Xbox 360 controller is currently connected and responding.
    /// </summary>
    /// <value>
    /// <c>true</c> if controller is connected and communication is active; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// Connection status is updated during polling operations and reflects real-time device availability.
    /// </remarks>
    public bool IsControllerConnected => _isControllerConnected;

    #endregion

    #region Private Fields

    /// <summary>
    /// Reactive subject for horizontal movement events from left stick X-axis.
    /// </summary>
    private readonly BehaviorSubject<double> _horizontalMovementSubject = new(0.0);
    
    /// <summary>
    /// Reactive subject for vertical movement events from combined trigger inputs.
    /// </summary>
    private readonly BehaviorSubject<double> _verticalMovementSubject = new(0.0);
    
    /// <summary>
    /// Reactive subject for button press events from Xbox 360 action buttons.
    /// </summary>
    private readonly BehaviorSubject<Buttons?> _buttonPressedSubject = new(null);
    
    /// <summary>
    /// Background task handle for controller input polling operations.
    /// </summary>
    private Task? _controllerMonitoringTask;
    
    /// <summary>
    /// Cancellation token source for graceful termination of controller monitoring.
    /// </summary>
    private CancellationTokenSource? _controllerMonitoringCts;
    
    /// <summary>
    /// Current connection status of the Xbox 360 controller.
    /// </summary>
    private bool _isControllerConnected;
    
    /// <summary>
    /// Disposal status flag to prevent multiple disposal operations.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// File stream for reading Linux input events from the controller device.
    /// </summary>
    private FileStream? _controllerInputStream;
    
    /// <summary>
    /// Linux device path to the Xbox 360 controller input event file.
    /// </summary>
    private string? _controllerDevicePath;

    /// <summary>
    /// Previously pressed buttons for edge detection, replaces per-button booleans.
    /// </summary>
    private HashSet<Buttons> _pressedButtonsPrev = new();

    /// <summary>
    /// Current controller state containing all analog and digital input values.
    /// </summary>
    private readonly ControllerState _currentState = new();

    /// <summary>
    /// Optional logger for diagnostics; falls back to Console if null.
    /// </summary>
    private readonly ILogger<RemoteControllerManager>? _logger;

    #endregion

    #region Private Enums

    /// <summary>
    /// Minimum change threshold for movement values to reduce noise and prevent excessive updates.
    /// </summary>
    /// <value>0.1 (10% change required to trigger observable emission)</value>
    private const double MovementThreshold = 0.1;

    /// <summary>
    /// Polling interval for controller input monitoring in milliseconds.
    /// </summary>
    /// <value>50ms for responsive input handling (20 Hz update rate)</value>
    private const int PollingIntervalMs = 50;

    /// <summary>
    /// Dead zone radius for left stick to prevent drift and unintended movement.
    /// </summary>
    /// <value>0.15 (15% of full range around center position)</value>
    private const double StickDeadZone = 0.15;

    /// <summary>
    /// Dead zone threshold for triggers to ensure clean zero state.
    /// </summary>
    /// <value>0.05 (5% of full trigger range)</value>
    private const double TriggerDeadZone = 0.05;

    /// <summary>
    /// Linux input event types (subset).
    /// </summary>
    private enum EventType : ushort
    {
        Key = 0x01,
        Abs = 0x03
    }

    /// <summary>
    /// Linux ABS axis codes used by Xbox 360 controller.
    /// </summary>
    private enum AbsCode : ushort
    {
        X = 0,   // Left stick X
        Z = 2,   // Left trigger (LT)
        RZ = 5   // Right trigger (RT)
    }

    /// <summary>
    /// Linux button codes used by Xbox 360 controller.
    /// </summary>
    private enum BtnCode : ushort
    {
        A = 304,
        B = 305,
        X = 307,
        Y = 308
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControllerManager"/> class for Xbox 360 controller integration.
    /// </summary>
    /// <remarks>
    /// The constructor automatically starts controller monitoring to begin processing input events.
    /// Controller detection and connection establishment occurs asynchronously in the background.
    /// </remarks>
    public RemoteControllerManager()
    {
        StartMonitoring();
    }

    /// <summary>
    /// Initializes a new instance with an injected logger.
    /// </summary>
    public RemoteControllerManager(ILogger<RemoteControllerManager> logger)
    {
        _logger = logger;
        StartMonitoring();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Starts monitoring the Xbox 360 controller for input events in a background task.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when controller monitoring is already active and running.
    /// </exception>
    /// <remarks>
    /// Creates a background task that continuously polls for controller input at the specified interval.
    /// The task handles controller detection, connection management, and input event processing.
    /// </remarks>
    public void StartMonitoring()
    {
        // Prevent duplicate monitoring tasks
        if (_controllerMonitoringTask is { IsCompleted: false })
            throw new InvalidOperationException("Xbox 360 controller monitoring is already active.");

        // Create cancellation token for graceful shutdown
        _controllerMonitoringCts = new CancellationTokenSource();
        
        // Start background monitoring task
        _controllerMonitoringTask = Task.Run(async () =>
        {
            // Continue monitoring until cancellation is requested
            while (!_controllerMonitoringCts.Token.IsCancellationRequested)
            {
                try
                {
                    // Poll controller for new input events
                    await PollControllerInput();
                    
                    // Wait for next polling cycle (maintains 20 Hz update rate)
                    await Task.Delay(PollingIntervalMs, _controllerMonitoringCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected exception during shutdown - exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    // Log unexpected errors and mark controller as disconnected
                    LogWarn($"Xbox 360 controller monitoring error: {ex.Message}");
                    _isControllerConnected = false;
                }
            }
        });
    }

    /// <summary>
    /// Stops monitoring the Xbox 360 controller and releases associated resources.
    /// </summary>
    /// <remarks>
    /// Gracefully terminates the background monitoring task and resets movement observables to neutral position.
    /// Waits up to 2 seconds for the monitoring task to complete before forcing termination.
    /// </remarks>
    public void StopMonitoring()
    {
        if (_controllerMonitoringCts != null)
        {
            // Signal the monitoring task to terminate
            _controllerMonitoringCts.Cancel();
            
            // Wait for graceful shutdown (max 2 seconds)
            _controllerMonitoringTask?.Wait(TimeSpan.FromSeconds(2));
            
            // Clean up cancellation token resources
            _controllerMonitoringCts.Dispose();
            _controllerMonitoringCts = null;
        }

        // Reset connection state and movement values
        _isControllerConnected = false;
        _horizontalMovementSubject.OnNext(0.0);  // Reset to neutral horizontal position
        _verticalMovementSubject.OnNext(0.0);    // Reset to neutral vertical position
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Polls the Xbox 360 controller for current input state and processes any changes.
    /// </summary>
    /// <returns>A task representing the asynchronous polling operation.</returns>
    /// <remarks>
    /// This method performs the core input processing cycle:
    /// 1. Ensures controller connection is established
    /// 2. Reads pending input events from the device
    /// 3. Processes movement changes through dead zone and threshold filtering
    /// 4. Emits observable events for significant changes
    /// 5. Processes button state changes
    /// </remarks>
    private async Task PollControllerInput()
    {
        // Verify controller connection is active
        if (!EnsureControllerConnected())
        {
            _isControllerConnected = false;
            return;
        }

        _isControllerConnected = true;

        // Read and process any pending input events from the device
        await ReadControllerEvents();

        // Process horizontal movement from left stick X-axis
        var horizontal = ApplyDeadZone(_currentState.LeftStickX, StickDeadZone);
        
        // Only emit observable event if change exceeds movement threshold (reduces noise)
            if (Math.Abs(horizontal - _horizontalMovementSubject.Value) > MovementThreshold)
        {
            // Invert value to be in line with other components's coordinate system.
            _horizontalMovementSubject.OnNext(-horizontal);
        }

        // Process vertical movement by combining both trigger inputs
        var rightTrigger = ApplyDeadZone(_currentState.RightTrigger, TriggerDeadZone);
        var leftTrigger = ApplyDeadZone(_currentState.LeftTrigger, TriggerDeadZone);
        
        // Calculate combined vertical movement: RT gives positive (forward), LT gives negative (backward)
        var vertical = rightTrigger - leftTrigger;
        
        // Only emit observable event if change exceeds movement threshold
    if (Math.Abs(vertical - _verticalMovementSubject.Value) > MovementThreshold)
        {
            _verticalMovementSubject.OnNext(vertical);
        }

        // Process button press/release state changes
        ProcessButtonChanges(_currentState);
    }

    /// <summary>
    /// Processes Xbox 360 controller button state changes and emits events for newly pressed buttons.
    /// </summary>
    /// <param name="state">Current controller state containing button press information.</param>
    /// <remarks>
    /// Uses edge detection to identify button press events (transition from released to pressed).
    /// Only emits observable events on button press, not release, to prevent duplicate actions.
    /// Button mappings: Xbox A→ButtonA, Xbox B→ButtonB, Xbox X→ButtonX, Xbox Y→ButtonY.
    /// </remarks>
    private void ProcessButtonChanges(ControllerState state)
    {
        // Build current pressed set
        var pressedNow = new HashSet<Buttons>();
        if (state.AButton) pressedNow.Add(Buttons.ButtonA);
        if (state.BButton) pressedNow.Add(Buttons.ButtonB);
        if (state.XButton) pressedNow.Add(Buttons.ButtonX);
        if (state.YButton) pressedNow.Add(Buttons.ButtonY);

        // Emit only newly pressed buttons (edge detection)
        foreach (var newlyPressed in pressedNow)
        {
            if (!_pressedButtonsPrev.Contains(newlyPressed))
            {
                _buttonPressedSubject.OnNext(newlyPressed);
            }
        }

        // Update snapshot
        _pressedButtonsPrev = pressedNow;
    }

    /// <summary>
    /// Applies dead zone filtering to raw controller input values to eliminate drift and noise.
    /// </summary>
    /// <param name="value">Raw input value from controller hardware.</param>
    /// <param name="deadZone">Dead zone radius as a fraction of full range (0.0 to 1.0).</param>
    /// <returns>
    /// Filtered input value with dead zone applied. Returns 0.0 if input falls within dead zone,
    /// otherwise returns scaled value that accounts for the dead zone reduction.
    /// </returns>
    /// <remarks>
    /// Dead zone processing prevents unintended movement from controller drift and provides
    /// a smooth transition from zero to active input. The scaling ensures full range utilization
    /// outside the dead zone while maintaining proportional response.
    /// </remarks>
    private static double ApplyDeadZone(double value, double deadZone)
    {
        // If input magnitude is within dead zone, return zero (no movement)
    if (Math.Abs(value) < deadZone)
            return 0.0;

        // Calculate scaling to compensate for dead zone reduction
        var sign = Math.Sign(value);  // Preserve input direction
        var scaledValue = (Math.Abs(value) - deadZone) / (1.0 - deadZone);  // Scale remaining range to full 0-1
        
        // Ensure result doesn't exceed normalized range and restore original sign
        return sign * Math.Min(scaledValue, 1.0);
    }

    /// <summary>
    /// Ensures that an Xbox 360 controller connection is established and ready for input.
    /// </summary>
    /// <returns>
    /// <c>true</c> if controller is connected and communication stream is active; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Performs controller discovery, device path resolution, and input stream initialization.
    /// Maintains existing connections when possible to avoid unnecessary reconnection overhead.
    /// </remarks>
    private bool EnsureControllerConnected()
    {
        // If controller is already connected and stream is active, no action needed
        if (_controllerInputStream != null && _isControllerConnected)
        {
            return true;
        }

        // Discover Xbox 360 controller device path in Linux input system
        _controllerDevicePath = FindXbox360Controller();
        if (string.IsNullOrEmpty(_controllerDevicePath))
        {
            return false;  // No controller found
        }

        try
        {
            // Open file stream to Linux input event device for reading raw input events
            _controllerInputStream = new FileStream(_controllerDevicePath, FileMode.Open, FileAccess.Read);
            LogInfo($"Connected to Xbox 360 controller: {_controllerDevicePath}");
            return true;
        }
        catch (Exception ex)
        {
            // Handle connection failures (permissions, device busy, etc.)
            LogWarn($"Failed to connect to Xbox 360 controller: {ex.Message}");
            
            // Clean up failed connection attempt
            _controllerInputStream?.Dispose();
            _controllerInputStream = null;
            return false;
        }
    }

    /// <summary>
    /// Finds the Xbox 360 controller device path within the Linux input subsystem.
    /// </summary>
    /// <returns>
    /// Device path string (e.g., "/dev/input/event2") if Xbox 360 controller is found; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// Uses two detection methods for reliable controller identification:
    /// 1. Device name matching for common Xbox 360 controller identifiers
    /// 2. Hardware vendor/product ID verification (Microsoft 045e:028e/028f)
    /// Scans all available input event devices in /dev/input/event* sequence.
    /// </remarks>
    private static string? FindXbox360Controller()
    {
        try
        {
            // Enumerate all Linux input event devices
            var eventDevices = Directory.GetFiles("/dev/input", "event*");
            
            foreach (var device in eventDevices)
            {
                try
                {
                    // Method 1: Check device name for Xbox 360 controller identifiers
                    var nameFile = $"/sys/class/input/{Path.GetFileName(device)}/device/name";
                    if (File.Exists(nameFile))
                    {
                        var deviceName = File.ReadAllText(nameFile).Trim().ToLowerInvariant();
                        
                        // Match common Xbox 360 controller device name patterns
                        if (deviceName.Contains("xbox 360") || 
                            deviceName.Contains("microsoft xbox 360") ||
                            deviceName.Contains("xbox360") ||
                            deviceName.Contains("gamepad"))
                        {
                            // Note: logging occurs at higher level after connection
                            return device;
                        }
                    }
                    
                    // Method 2: Verify using hardware vendor/product identification
                    var vendorFile = $"/sys/class/input/{Path.GetFileName(device)}/device/id/vendor";
                    var productFile = $"/sys/class/input/{Path.GetFileName(device)}/device/id/product";
                    
                    if (File.Exists(vendorFile) && File.Exists(productFile))
                    {
                        var vendorId = File.ReadAllText(vendorFile).Trim();
                        var productId = File.ReadAllText(productFile).Trim();
                        
                        // Verify Microsoft vendor ID and Xbox 360 product IDs
                        if (vendorId.Equals("045e", StringComparison.OrdinalIgnoreCase) &&
                            (productId.Equals("028e", StringComparison.OrdinalIgnoreCase) ||  // Wired Xbox 360
                             productId.Equals("028f", StringComparison.OrdinalIgnoreCase)))   // Wireless Xbox 360
                        {
                            // Note: logging occurs at higher level after connection
                            return device;
                        }
                    }
                }
                catch
                {
                    // Skip devices that cannot be accessed (permissions, etc.)
                    continue;
                }
            }
        }
    catch (Exception)
        {
            // We cannot log here without an instance; leave to caller side
        }

        // No Xbox 360 controller detected
        return null;
    }

    /// <summary>
    /// Reads pending input events from the Xbox 360 controller device and updates the current state.
    /// </summary>
    /// <returns>A task representing the asynchronous read operation.</returns>
    /// <remarks>
    /// Reads Linux input_event structures (24 bytes each) from the device file stream.
    /// Each event contains timing information, event type/code, and value data.
    /// Handles read errors by disconnecting and allowing reconnection attempts.
    /// </remarks>
    private async Task ReadControllerEvents()
    {
        if (_controllerInputStream == null) return;

        try
        {
            // Read Linux input_event structure: [time_sec, time_usec, type, code, value] = 24 bytes
            var buffer = new byte[24];  // input_event is 24 bytes on 64-bit Linux systems
            var token = _controllerMonitoringCts?.Token ?? CancellationToken.None;
            var bytesRead = await _controllerInputStream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
            
            // Verify complete event was read
            if (bytesRead == 24)
            {
                // Extract event data from binary structure
                var type = BitConverter.ToUInt16(buffer, 16);   // Event type (key, absolute, etc.)
                var code = BitConverter.ToUInt16(buffer, 18);   // Specific input identifier
                var value = BitConverter.ToInt32(buffer, 20);   // Input value

                // Process the input event and update controller state
                ProcessInputEvent(type, code, value);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown; treat as clean exit from read
        }
        catch (Exception ex)
        {
            // Handle device communication errors (device removed, permissions, etc.)
            LogWarn($"Error reading controller input: {ex.Message}");
            
            // Disconnect to allow reconnection attempts
            _controllerInputStream?.Dispose();
            _controllerInputStream = null;
            _isControllerConnected = false;
        }
    }

    /// <summary>
    /// Processes a raw Linux input event and delegates to appropriate handler based on event type.
    /// </summary>
    /// <param name="type">The Linux input event type (EV_KEY for buttons, EV_ABS for analog inputs).</param>
    /// <param name="code">The specific input code identifying which button or axis generated the event.</param>
    /// <param name="value">The input value (0/1 for buttons, calibrated range for analog inputs).</param>
    /// <remarks>
    /// Linux input subsystem uses typed events: EV_KEY (0x01) for digital buttons,
    /// EV_ABS (0x03) for analog axes. Other event types are ignored.
    /// </remarks>
    private void ProcessInputEvent(ushort type, ushort code, int value)
    {
        // Delegate to specific handlers based on Linux input event type
        switch ((EventType)type)
        {
            case EventType.Key: // Digital button press/release events
                ProcessButtonEvent(code, value != 0);
                break;
                
            case EventType.Abs: // Analog axis/trigger movement events
                ProcessAxisEvent(code, value);
                break;
                
            // Ignore other event types (EV_SYN, EV_REL, etc.)
        }
    }

    /// <summary>
    /// Processes button press/release events from the Xbox 360 controller.
    /// </summary>
    /// <param name="code">The Linux input button code (BTN_A, BTN_B, etc.).</param>
    /// <param name="pressed">True if the button was pressed, false if released.</param>
    /// <remarks>
    /// Maps Xbox 360 hardware button codes to TriloBot button enumeration.
    /// Updates controller state and publishes events to observers.
    /// </remarks>
    private void ProcessButtonEvent(ushort code, bool pressed)
    {
        // Map Linux input button codes to controller state
        switch ((BtnCode)code)
        {
            case BtnCode.A:  // Xbox A button (bottom face button)
                _currentState.AButton = pressed;
                break;
            case BtnCode.B:  // Xbox B button (right face button)
                _currentState.BButton = pressed;
                break;
            case BtnCode.X:  // Xbox X button (left face button)
                _currentState.XButton = pressed;
                break;
            case BtnCode.Y:  // Xbox Y button (top face button)
                _currentState.YButton = pressed;
                break;
                
            // Ignore unmapped buttons (shoulders, d-pad, etc.)
        }
    }

    /// <summary>
    /// Processes analog stick and trigger events from the Xbox 360 controller.
    /// </summary>
    /// <param name="code">The Linux input axis code (ABS_X, ABS_Z, ABS_RZ, etc.).</param>
    /// <param name="value">The raw axis value from the hardware device.</param>
    /// <remarks>
    /// Xbox 360 analog inputs have different value ranges:
    /// - Left stick X: -32768 to 32767 (signed 16-bit)
    /// - Triggers: 0 to 255 (unsigned 8-bit)
    /// Values are normalized to standard ranges and clamped for safety.
    /// </remarks>
    private void ProcessAxisEvent(ushort code, int value)
    {
        // Process different Xbox 360 analog inputs with hardware-specific normalization
        switch ((AbsCode)code)
        {
            case AbsCode.X: // Left stick X-axis (horizontal movement control)
                // Normalize from Xbox 360 range (-32768 to 32767) to standard range (-1.0 to 1.0)
                _currentState.LeftStickX = Math.Clamp(value / 32767.0, -1.0, 1.0);
                break;
                
            case AbsCode.Z: // Left trigger (LT) - backward movement control
                // Normalize from Xbox 360 range (0 to 255) to standard range (0.0 to 1.0)
                _currentState.LeftTrigger = Math.Clamp(value / 255.0, 0.0, 1.0);
                break;
                
            case AbsCode.RZ: // Right trigger (RT) - forward movement control
                // Normalize from Xbox 360 range (0 to 255) to standard range (0.0 to 1.0)
                _currentState.RightTrigger = Math.Clamp(value / 255.0, 0.0, 1.0);
                break;
                
            // Ignore unmapped axes (right stick, d-pad, etc.)
        }
    }

    #endregion

    #region IDisposable Support

    /// <summary>
    /// Releases all resources used by the RemoteControllerManager.
    /// </summary>
    /// <remarks>
    /// Implements IDisposable pattern to properly clean up system resources:
    /// - Stops background monitoring task and cancels operations
    /// - Closes file stream to Xbox 360 controller device
    /// - Disposes reactive subjects to prevent memory leaks
    /// Safe to call multiple times.
    /// </remarks>
    public void Dispose()
    {
        // Prevent multiple disposal attempts
        if (_disposed) return;

        // Stop background monitoring and cancel pending operations
        StopMonitoring();
        
        // Close device file stream and release handle
        _controllerInputStream?.Dispose();
        _controllerInputStream = null;
        
        // Dispose reactive subjects to complete observable chains
        _horizontalMovementSubject.Dispose();
        _verticalMovementSubject.Dispose();
        _buttonPressedSubject.Dispose();

        // Mark as disposed to prevent further use
        _disposed = true;
    }

    #endregion

    #region Logging helpers
    private void LogInfo(string message)
    {
        if (_logger != null) _logger.LogInformation(message);
        else Console.WriteLine(message);
    }

    private void LogWarn(string message)
    {
        if (_logger != null) _logger.LogWarning(message);
        else Console.WriteLine(message);
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
    /// Left trigger value (0.0 to 1.0) from Xbox 360 controller.
    /// </summary>
    public double LeftTrigger { get; set; }

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