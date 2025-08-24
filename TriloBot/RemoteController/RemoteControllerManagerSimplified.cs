using System.Reactive.Linq;
using System.Reactive.Subjects;
using TriloBot.Button;
using Microsoft.Extensions.Logging;

namespace TriloBot.RemoteController;

/// <summary>
/// Simplified Xbox controller manager that delegates complexity to specialized classes.
/// Manages controller input processing using strategy pattern and separation of concerns.
/// </summary>
public sealed class RemoteControllerManagerSimplified : IDisposable
{
    #region Public Properties

    /// <summary>
    /// Observable that emits horizontal movement values from the left stick X-axis.
    /// Values are normalized to range (-1.0 to 1.0) with dead zone filtering applied.
    /// </summary>
    public IObservable<double> HorizontalMovementObservable => _horizontalMovementSubject.AsObservable();

    /// <summary>
    /// Observable that emits vertical movement values from combined trigger inputs.
    /// Values are normalized to range (-1.0 to 1.0), calculated as rightTrigger - leftTrigger.
    /// </summary>
    public IObservable<double> VerticalMovementObservable => _verticalMovementSubject.AsObservable();

    /// <summary>
    /// Observable that emits button press events for Xbox controller action buttons.
    /// Emits only on button press (not release) to prevent duplicate events.
    /// </summary>
    public IObservable<Buttons?> ButtonPressedObservable => _buttonPressedSubject.AsObservable();

    /// <summary>
    /// Gets a value indicating whether an Xbox controller is currently connected.
    /// </summary>
    public bool IsControllerConnected => _connectionManager.IsConnected;

    #endregion

    #region Private Fields

    private readonly BehaviorSubject<double> _horizontalMovementSubject = new(0.0);
    private readonly BehaviorSubject<double> _verticalMovementSubject = new(0.0);
    private readonly BehaviorSubject<Buttons?> _buttonPressedSubject = new(null);
    
    private readonly ControllerConnectionManager _connectionManager = new();
    private readonly IControllerStrategy _controllerStrategy;
    private readonly SharedControllerState _currentState = new();
    private readonly ILogger<RemoteControllerManagerSimplified>? _logger;
    
    private Task? _monitoringTask;
    private CancellationTokenSource? _monitoringCts;
    private HashSet<Buttons> _pressedButtonsPrev = new();
    private int _ltMax;
    private int _rtMax;
    private bool _disposed;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the simplified RemoteControllerManager.
    /// </summary>
    /// <param name="controllerType">The type of Xbox controller to support.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public RemoteControllerManagerSimplified(ControllerType controllerType, ILogger<RemoteControllerManagerSimplified>? logger = null)
    {
        _logger = logger;
        _controllerStrategy = CreateStrategy(controllerType);
        (_ltMax, _rtMax) = _controllerStrategy.GetInitialTriggerRanges();
        StartMonitoring();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Starts monitoring the Xbox controller for input events.
    /// </summary>
    public void StartMonitoring()
    {
        if (_monitoringTask is { IsCompleted: false })
            throw new InvalidOperationException("Controller monitoring is already active.");

        _monitoringCts = new CancellationTokenSource();
        _monitoringTask = Task.Run(MonitoringLoop);
    }

    /// <summary>
    /// Stops monitoring the Xbox controller and resets movement values.
    /// </summary>
    public void StopMonitoring()
    {
        if (_monitoringCts != null)
        {
            _monitoringCts.Cancel();
            _monitoringTask?.Wait(TimeSpan.FromSeconds(ControllerConfiguration.ShutdownTimeoutSeconds));
            _monitoringCts.Dispose();
            _monitoringCts = null;
        }

        _connectionManager.Disconnect();
        ResetMovementValues();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Main monitoring loop that processes controller input continuously.
    /// </summary>
    private async Task MonitoringLoop()
    {
        while (!_monitoringCts?.Token.IsCancellationRequested ?? false)
        {
            try
            {
                await ProcessControllerInput();
                await Task.Delay(ControllerConfiguration.PollingIntervalMs, _monitoringCts?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogWarning($"Controller monitoring error: {ex.Message}");
                _connectionManager.Disconnect();
            }
        }
    }

    /// <summary>
    /// Processes controller input for a single polling cycle.
    /// </summary>
    private async Task ProcessControllerInput()
    {
        if (!_connectionManager.EnsureConnected())
            return;

        var inputEvent = await _connectionManager.ReadEventAsync(_monitoringCts?.Token ?? CancellationToken.None);
        if (inputEvent == null)
            return;

        ProcessInputEvent(inputEvent);
        UpdateMovementObservables();
        UpdateButtonObservables();
    }

    /// <summary>
    /// Processes a single input event and updates controller state.
    /// </summary>
    private void ProcessInputEvent(InputEvent inputEvent)
    {
        switch ((LinuxInputConstants.EventType)inputEvent.Type)
        {
            case LinuxInputConstants.EventType.Key:
                ProcessButtonEvent(inputEvent.Code, inputEvent.Value != 0);
                break;
                
            case LinuxInputConstants.EventType.Abs:
                _controllerStrategy.ProcessAxisEvent(inputEvent.Code, inputEvent.Value, _currentState, ref _ltMax, ref _rtMax);
                break;
        }
    }

    /// <summary>
    /// Processes button press/release events.
    /// </summary>
    private void ProcessButtonEvent(ushort code, bool pressed)
    {
        switch ((LinuxInputConstants.BtnCode)code)
        {
            case LinuxInputConstants.BtnCode.A:
                _currentState.AButton = pressed;
                break;
            case LinuxInputConstants.BtnCode.B:
                _currentState.BButton = pressed;
                break;
            case LinuxInputConstants.BtnCode.X:
                _currentState.XButton = pressed;
                break;
            case LinuxInputConstants.BtnCode.Y:
                _currentState.YButton = pressed;
                break;
        }
    }

    /// <summary>
    /// Updates movement observables with dead zone filtering and threshold detection.
    /// </summary>
    private void UpdateMovementObservables()
    {
        // Process horizontal movement
        var horizontal = ApplyDeadZone(_currentState.LeftStickX, ControllerConfiguration.StickDeadZone);
        if (Math.Abs(horizontal - _horizontalMovementSubject.Value) > ControllerConfiguration.MovementThreshold)
        {
            _horizontalMovementSubject.OnNext(-horizontal); // Invert for coordinate system consistency
        }

        // Process vertical movement
        var rightTrigger = ApplyDeadZone(_currentState.RightTrigger, ControllerConfiguration.TriggerDeadZone);
        var leftTrigger = ApplyDeadZone(_currentState.LeftTrigger, ControllerConfiguration.TriggerDeadZone);
        var vertical = rightTrigger - leftTrigger;
        
        if (Math.Abs(vertical - _verticalMovementSubject.Value) > ControllerConfiguration.MovementThreshold)
        {
            _verticalMovementSubject.OnNext(vertical);
        }
    }

    /// <summary>
    /// Updates button press observables with edge detection.
    /// </summary>
    private void UpdateButtonObservables()
    {
        var pressedNow = GetCurrentlyPressedButtons();
        
        // Emit only newly pressed buttons (edge detection)
        foreach (var newlyPressed in pressedNow)
        {
            if (!_pressedButtonsPrev.Contains(newlyPressed))
            {
                _buttonPressedSubject.OnNext(newlyPressed);
            }
        }

        _pressedButtonsPrev = pressedNow;
    }

    /// <summary>
    /// Gets the set of currently pressed buttons.
    /// </summary>
    private HashSet<Buttons> GetCurrentlyPressedButtons()
    {
        var pressed = new HashSet<Buttons>();
        if (_currentState.AButton) pressed.Add(Buttons.ButtonA);
        if (_currentState.BButton) pressed.Add(Buttons.ButtonB);
        if (_currentState.XButton) pressed.Add(Buttons.ButtonX);
        if (_currentState.YButton) pressed.Add(Buttons.ButtonY);
        return pressed;
    }

    /// <summary>
    /// Applies dead zone filtering to controller input values.
    /// </summary>
    private static double ApplyDeadZone(double value, double deadZone)
    {
        if (Math.Abs(value) < deadZone)
            return 0.0;

        var sign = Math.Sign(value);
        var scaledValue = (Math.Abs(value) - deadZone) / (1.0 - deadZone);
        return sign * Math.Min(scaledValue, 1.0);
    }

    /// <summary>
    /// Creates the appropriate controller strategy based on the controller type.
    /// </summary>
    private static IControllerStrategy CreateStrategy(ControllerType controllerType)
    {
        return controllerType switch
        {
            ControllerType.Xbox360 => new Xbox360Strategy(),
            ControllerType.XboxSeries => new XboxSeriesStrategy(),
            _ => throw new ArgumentException($"Unsupported controller type: {controllerType}")
        };
    }

    /// <summary>
    /// Resets movement observable values to neutral position.
    /// </summary>
    private void ResetMovementValues()
    {
        _horizontalMovementSubject.OnNext(0.0);
        _verticalMovementSubject.OnNext(0.0);
    }

    /// <summary>
    /// Logs a warning message using the configured logger or console.
    /// </summary>
    private void LogWarning(string message)
    {
        if (_logger != null)
            _logger.LogWarning(message);
        else
            Console.WriteLine($"Warning: {message}");
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Releases all resources used by the RemoteControllerManager.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        StopMonitoring();
        _connectionManager.Dispose();
        
        _horizontalMovementSubject.Dispose();
        _verticalMovementSubject.Dispose();
        _buttonPressedSubject.Dispose();

        _disposed = true;
    }

    #endregion
}
