using System.Device.Gpio;
using System.Runtime.InteropServices;
using System.Reactive.Subjects;
using TriloBot.Light;
using TriloBot.Light.Modes;
using TriloBot.Motor;
using TriloBot.Ultrasound;

namespace TriloBot;

/// <summary>
/// Main class for controlling the TriloBot robot, providing access to buttons, lights, motors, and ultrasound sensors.
/// </summary>
public class TriloBot : IDisposable
{
    /// <summary>
    /// Observable that indicates if an object is too near (distance below 10).
    /// </summary>
    public BehaviorSubject<bool> objectTooNearObserver = new(false);
    /// <summary>
    /// Tracks whether the object has been disposed.
    /// </summary>
    private bool _disposed = false;
    /// <summary>
    /// Observable for the latest distance readings.
    /// </summary>
    public BehaviorSubject<double> distanceObserver = new(0.0);

    /// <summary>
    /// Task for background distance monitoring.
    /// </summary>
    private Task? _distanceMonitoringTask;

    /// <summary>
    /// CancellationTokenSource for distance monitoring.
    /// </summary>
    private CancellationTokenSource? _distanceMonitoringCts;

    #region Private Fields

    /// <summary>
    /// Cancellation token for managing asynchronous operations.
    /// This allows for graceful shutdown of effects and operations.
    /// It can be used to cancel ongoing tasks like light effects or motor operations.
    /// This is particularly useful for effects that run in a loop or for long-running operations.
    /// The cancellation token can be passed to methods that support cancellation, allowing them to stop gracefully.
    /// </summary>
    private CancellationToken _cancellationToken;

    /// <summary>
    /// The GPIO controller used for all hardware operations.
    /// </summary>
    private readonly GpioController _gpio;

    /// <summary>
    /// Manages button input operations.
    /// </summary>
    private readonly Button.ButtonManager _buttonManager = null!;

    /// <summary>
    /// Manages LED and underlighting operations.
    /// </summary>
    private readonly LightManager _lightManager = null!;

    /// <summary>
    /// Manages motor control operations.
    /// </summary>
    private readonly MotorManager _motorManager = null!;

    /// <summary>
    /// Manages ultrasound distance measurement operations.
    /// </summary>
    private readonly UltrasoundManager _ultrasoundManager = null!;

    #endregion

    #region Distance Monitoring

    /// <summary>
    /// Starts non-blocking background monitoring of the distance sensor every 500ms.
    /// </summary>
    public void StartDistanceMonitoring()
    {
        // If already running, do nothing
        if (_distanceMonitoringTask != null && !_distanceMonitoringTask.IsCompleted)
            return;

        _distanceMonitoringCts = new CancellationTokenSource();
        var token = _distanceMonitoringCts.Token;

        _distanceMonitoringTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Read and publish distance
                    var distance = _ultrasoundManager.ReadDistance();
                    distanceObserver.OnNext(distance);
                    // Publish object too near state
                    objectTooNearObserver.OnNext(distance < 10);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Distance monitoring error: {ex.Message}");
                }
                await Task.Delay(500, token);
            }
        }, token);
    }

    /// <summary>
    /// Stops the background distance monitoring task if running.
    /// </summary>
    public void StopDistanceMonitoring()
    {
        if (_distanceMonitoringCts != null)
        {
            _distanceMonitoringCts.Cancel();
            try
            {
                _distanceMonitoringTask?.Wait(1000);
            }
            catch (AggregateException) { }
            catch (OperationCanceledException) { }
            finally
            {
                _distanceMonitoringCts.Dispose();
                _distanceMonitoringCts = null;
                _distanceMonitoringTask = null;
            }
        }
    }

    #endregion



    #region  Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="TriloBot"/> class and sets up all subsystems.
    /// </summary>
    public TriloBot(CancellationToken cancellationToken = default)
    {
        // Check if the current platform is supported
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) == false)
        {
            throw new PlatformNotSupportedException("TriloBot is only supported on Raspberry Pi platforms.");
        }

        // Set the cancellation token for this instance
        _cancellationToken = cancellationToken;

        // Initialize GPIO controller
        // This is the main controller for all GPIO operations
        // It manages the pins used for buttons, lights, motors, and ultrasound sensors
        // It is initialized once and used throughout the TriloBot operations
        // This allows for efficient resource management and avoids multiple initializations
        _gpio = new GpioController();

        // Setup button manager
        _buttonManager = new Button.ButtonManager(_gpio);

        // Setup light manager
        _lightManager = new LightManager(_gpio);

        // Setup motor manager
        _motorManager = new MotorManager(_gpio);

        // Setup ultrasound manager
        _ultrasoundManager = new UltrasoundManager(_gpio);

        // Register cancellation token to handle graceful shutdown
        // This allows the TriloBot to clean up resources and stop ongoing operations when cancellation is requested
        // It ensures that any ongoing effects or operations are properly terminated
        _cancellationToken.Register(() =>
        {
            // Cancel any ongoing effects or operations
            DisableUnderlighting();
            DisableMotors();
            // Do not call Dispose() here; handled by using statement
        });

        // Log successful initialization
        Console.WriteLine("Successfully initialized TrilBot manager");
    }

    #endregion

    #region Button methods

    /// <summary>
    /// Reads the state of a button.
    /// </summary>
    /// <param name="button">The index of the button (0-3).</param>
    /// <returns>True if the button is pressed, otherwise false.</returns>
    public bool ReadButton(int button) => _buttonManager.ReadButton(button);

    /// <summary>
    /// Sets the brightness of a button LED.
    /// </summary>
    /// <param name="buttonLed">The index of the button LED (0-3).</param>
    /// <param name="value">Brightness value between 0.0 and 1.0.</param>
    public void SetButtonLed(int buttonLed, double value) => _lightManager.SetButtonLed(buttonLed, value);

    #endregion

    #region Motor methods

    // Motor control methods are now delegated to MotorManager
    /// <summary>Sets the speed and direction of a single motor.</summary>
    /// <param name="motor">The motor index (0 for left, 1 for right).</param>
    /// <param name="speed">Speed value between -1.0 (full reverse) and 1.0 (full forward).</param>
    public void SetMotorSpeed(int motor, double speed) => _motorManager.SetMotorSpeed(motor, speed);

    /// <summary>Sets the speed and direction of both motors.</summary>
    /// <param name="leftSpeed">Speed for the left motor (-1.0 to 1.0).</param>
    /// <param name="rightSpeed">Speed for the right motor (-1.0 to 1.0).</param>
    public void SetMotorSpeeds(double leftSpeed, double rightSpeed) => _motorManager.SetMotorSpeeds(leftSpeed, rightSpeed);

    /// <summary>Disables both motors and sets their PWM to 0.</summary>
    public void DisableMotors() => _motorManager.DisableMotors();

    /// <summary>Drives both motors forward at the specified speed.</summary>
    /// <param name="speed">Speed value (default 1.0).</param>
    public void Forward(double speed = 1.0) => _motorManager.Forward(speed);

    /// <summary>Drives both motors backward at the specified speed.</summary>
    /// <param name="speed">Speed value (default 1.0).</param>
    public void Backward(double speed = 1.0) => _motorManager.Backward(speed);

    /// <summary>Turns the robot left in place at the specified speed.</summary>
    /// <param name="speed">Speed value (default 1.0).</param>
    public void TurnLeft(double speed = 1.0) => _motorManager.TurnLeft(speed);

    /// <summary>Turns the robot right in place at the specified speed.</summary>
    /// <param name="speed">Speed value (default 1.0).</param>
    public void TurnRight(double speed = 1.0) => _motorManager.TurnRight(speed);

    /// <summary>Curves forward left (left motor stopped, right motor forward).</summary>
    /// <param name="speed">Speed value (default 1.0).</param>
    public void CurveForwardLeft(double speed = 1.0) => _motorManager.CurveForwardLeft(speed);

    /// <summary>Curves forward right (right motor stopped, left motor forward).</summary>
    /// <param name="speed">Speed value (default 1.0).</param>
    public void CurveForwardRight(double speed = 1.0) => _motorManager.CurveForwardRight(speed);

    /// <summary>Curves backward left (left motor stopped, right motor backward).</summary>
    /// <param name="speed">Speed value (default 1.0).</param>
    public void CurveBackwardLeft(double speed = 1.0) => _motorManager.CurveBackwardLeft(speed);

    /// <summary>Curves backward right (right motor stopped, left motor backward).</summary>
    /// <param name="speed">Speed value (default 1.0).</param>
    public void CurveBackwardRight(double speed = 1.0) => _motorManager.CurveBackwardRight(speed);

    /// <summary>Stops both motors (brake mode).</summary>
    public void Stop() => _motorManager.Stop();

    /// <summary>Disables both motors (coast mode).</summary>
    public void Coast() => _motorManager.Coast();

    #endregion

    #region Light methods

    /// <summary>Enables and displays the current underlighting values.</summary>
    public void ShowUnderlighting() => _lightManager.ShowUnderlighting();

    /// <summary>Disables the underlighting.</summary>
    public void DisableUnderlighting() => _lightManager.DisableUnderlighting();

    /// <summary>Sets the RGB value of a single underlight.</summary>
    /// <param name="light">The index of the underlight (0-5).</param>
    /// <param name="r">Red value (0-255).</param>
    /// <param name="g">Green value (0-255).</param>
    /// <param name="b">Blue value (0-255).</param>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void SetUnderlight(int light, byte r, byte g, byte b, bool show = true) => _lightManager.SetUnderlight(light, r, g, b, show);

    /// <summary>Sets the HSV value of a single underlight.</summary>
    /// <param name="light">The index of the underlight (0-5).</param>
    /// <param name="h">Hue value.</param>
    /// <param name="s">Saturation value.</param>
    /// <param name="v">Value (brightness).</param>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void SetUnderlightHsv(int light, double h, double s = 1.0, double v = 1.0, bool show = true) => _lightManager.SetUnderlightHsv(light, h, s, v, show);

    /// <summary>Fills all underlights with the specified RGB color.</summary>
    /// <param name="r">Red value (0-255).</param>
    /// <param name="g">Green value (0-255).</param>
    /// <param name="b">Blue value (0-255).</param>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void FillUnderlighting(byte r, byte g, byte b, bool show = true) => _lightManager.FillUnderlighting(r, g, b, show);

    /// <summary>Fills all underlights with the specified HSV color.</summary>
    /// <param name="h">Hue value.</param>
    /// <param name="s">Saturation value.</param>
    /// <param name="v">Value (brightness).</param>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void FillUnderlightingHsv(double h, double s = 1.0, double v = 1.0, bool show = true) => _lightManager.FillUnderlightingHsv(h, s, v, show);

    /// <summary>Clears a single underlight (sets it to off).</summary>
    /// <param name="light">The index of the underlight (0-5).</param>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void ClearUnderlight(int light, bool show = true) => _lightManager.ClearUnderlight(light, show);

    /// <summary>Clears all underlights (sets them to off).</summary>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void ClearUnderlighting(bool show = true) => _lightManager.ClearUnderlighting(show);

    /// <summary>Sets the RGB value for multiple underlights.</summary>
    /// <param name="lights">Array of underlight indices.</param>
    /// <param name="r">Red value (0-255).</param>
    /// <param name="g">Green value (0-255).</param>
    /// <param name="b">Blue value (0-255).</param>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void SetUnderlights(int[] lights, byte r, byte g, byte b, bool show = true) => _lightManager.SetUnderlights(lights, r, g, b, show);

    /// <summary>Sets the HSV value for multiple underlights.</summary>
    /// <param name="lights">Array of underlight indices.</param>
    /// <param name="h">Hue value.</param>
    /// <param name="s">Saturation value.</param>
    /// <param name="v">Value (brightness).</param>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void SetUnderlightsHsv(int[] lights, double h, double s = 1.0, double v = 1.0, bool show = true) => _lightManager.SetUnderlightsHsv(lights, h, s, v, show);

    /// <summary>Clears multiple underlights (sets them to off).</summary>
    /// <param name="lights">Array of underlight indices.</param>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void ClearUnderlights(int[] lights, bool show = true) => _lightManager.ClearUnderlights(lights, show);

    #endregion

    #region Ultrasound methods

    /// <summary>
    /// Reads the distance using the ultrasonic sensor.
    /// </summary>
    /// <param name="timeout">Timeout in milliseconds for each sample.</param>
    /// <param name="samples">Number of samples to average.</param>
    /// <returns>Distance

    #region Effect methods

    /// <summary>
    /// Plays the police lights effect on the underlighting.
    /// This effect alternates red and blue lights in a rotating pattern.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop the effect.</param>
    /// <remarks>
    /// The effect is cancelable using a cancellation token.
    /// </remarks>
    public void PlayPoliceEffect(CancellationToken cancellationToken = default)
    {
        LightModesExtensions.PoliceLightsEffect(_lightManager, cancellationToken);
    }

    #endregion

    /// <summary>
    /// Reads the distance from the ultrasonic sensor.
    /// </summary>
    /// <returns>Distance in centimeters</returns>
    public double ReadDistance()
        => _ultrasoundManager.ReadDistance();

    #endregion

    #region IDisposable Support

    /// <summary>
    /// Disposes the TriloBot and all its subsystems.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        System.Console.WriteLine("Disposing TriloBot...");

        try { DisableUnderlighting(); } catch { }
        try { DisableMotors(); } catch { }
        try { StopDistanceMonitoring(); } catch { }
        try { _motorManager?.Dispose(); } catch { }
        try { _buttonManager?.Dispose(); } catch { }
        try { _lightManager?.Dispose(); } catch { }
        try { _ultrasoundManager?.Dispose(); } catch { }
        try { _gpio.Dispose(); } catch { }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}
