using System.Device.Gpio;
using System.Runtime.InteropServices;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using TriloBot.Light;
using TriloBot.Motor;
using TriloBot.Ultrasound;
using TriloBot.Button;
using TriloBot.Camera;

namespace TriloBot;

/// <summary>
/// Main class for controlling the TriloBot robot, providing access to buttons, lights, motors, and ultrasound sensors.
/// </summary>
public class TriloBot : IDisposable
{
    /// <summary>
    /// Exposes the distance observer as an IObservable (read-only).
    /// </summary>
    public IObservable<double> DistanceObservable => _distanceObserver.AsObservable();

    /// <summary>
    /// Exposes the object-too-near observer as an IObservable (read-only).
    /// </summary>
    public IObservable<bool> ObjectTooNearObservable => _objectTooNearObserver.AsObservable();

    /// <summary>
    /// Exposes the button pressed observer as an IObservable (read-only).
    /// Emits the Buttons enum value when a button is pressed, or null if none.
    /// </summary>
    public IObservable<Buttons?> ButtonPressedObservable => _buttonPressedObserver.AsObservable();

    /// <summary>
    /// Exposes the live video feed URL as an observable. Emits a new value when the stream URL changes.
    /// </summary>
    public IObservable<string> LiveVideoFeedObservable => _liveVideoFeedSubject.AsObservable();

    /// <summary>
    /// Task for background button monitoring.
    /// </summary>
    private Task? _buttonMonitoringTask;

    /// <summary>
    /// Tracks whether the object has been disposed.
    /// </summary>
    private bool _disposed;

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
    /// The GPIO controller used for all hardware operations.
    /// </summary>
    private readonly GpioController _gpio;

    /// <summary>
    /// Manages button input operations.
    /// </summary>
    private readonly ButtonManager _buttonManager;

    /// <summary>
    /// Manages LED and underlighting operations.
    /// </summary>
    private readonly LightManager _lightManager;

    /// <summary>
    /// Manages motor control operations.
    /// </summary>
    private readonly MotorManager _motorManager;

    /// <summary>
    /// Manages ultrasound distance measurement operations.
    /// </summary>
    private readonly UltrasoundManager _ultrasoundManager;

    /// <summary>
    /// Manages camera operations.
    /// </summary>
    private readonly CameraManager _cameraManager;

    /// <summary>
    /// Subject for live video feed URL changes.
    /// </summary>
    private readonly BehaviorSubject<string> _liveVideoFeedSubject = new("");

    /// <summary>
    /// Observable that indicates if an object is too near (distance below 10).
    /// </summary>
    private readonly BehaviorSubject<bool> _objectTooNearObserver = new(false);

    /// <summary>
    /// Observable for the latest button press events.
    /// </summary>
    private readonly BehaviorSubject<Buttons?> _buttonPressedObserver = new(null);

    /// <summary>
    /// Observable for the latest distance readings.
    /// </summary>
    private readonly BehaviorSubject<double> _distanceObserver = new(0.0);

    /// <summary>
    /// CancellationTokenSource for button monitoring.
    /// </summary>
    private CancellationTokenSource? _buttonMonitoringCts;

    #endregion

    #region Distance Monitoring

    /// <summary>
    /// Starts non-blocking background monitoring of the distance sensor every 500 ms.
    /// </summary>
    public void StartDistanceMonitoring(double minDistance = 30.0)
    {
        // If already running, do nothing
        if (_distanceMonitoringTask is { IsCompleted: false })
            return;

        Console.WriteLine("Starting distance monitoring...");

        _distanceMonitoringCts = new CancellationTokenSource();
        var token = _distanceMonitoringCts.Token;

        _distanceMonitoringTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var distance = _ultrasoundManager.ReadDistance();
                    var isTooNear = distance < minDistance;

                    // Only trigger if the value changes
                    // This prevents unnecessary updates and notifications.
                    if (Math.Abs(_distanceObserver.Value - distance) > 0.1)
                    {
                        _distanceObserver.OnNext(distance);
                    }

                    if (_objectTooNearObserver.Value != isTooNear)
                    {
                        _objectTooNearObserver.OnNext(isTooNear);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Distance monitoring error: {ex.Message}");
                }
                await Task.Delay(500, token).ConfigureAwait(false);
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

        // Initialize GPIO controller
        // This is the main controller for all GPIO operations
        _gpio = new GpioController();

        // Setup button manager
        _buttonManager = new ButtonManager(_gpio);

        // Setup light manager
        _lightManager = new LightManager(_gpio);

        // Setup motor manager
        _motorManager = new MotorManager(_gpio);

        // Setup ultrasound manager
        _ultrasoundManager = new UltrasoundManager(_gpio);

        _cameraManager = new CameraManager();

        // Register cancellation token to handle graceful shutdown
        // This allows the TriloBot to clean up resources and stop ongoing operations when cancellation is requested
        // It ensures that any ongoing effects or operations are properly terminated
        cancellationToken.Register(() =>
        {
            // Cancel any ongoing effects or operations
            DisableUnderlighting();
            DisableMotors();
            // Do not call Dispose() here; handled by using statement
        });

        // Log successful initialization
        Console.WriteLine("Successfully initialized TriloBot manager. Start observer listing or triggering other methods.");
    }

    #endregion

    #region Button methods

    /// <summary>
    /// Reads the state of a button.
    /// </summary>
    /// <param name="button">The button enum value.</param>
    /// <returns>True if the button is pressed, otherwise false.</returns>
    public bool ReadButton(Buttons button) => _buttonManager.ReadButton(button);

    /// <summary>
    /// Sets the brightness of a button LED.
    /// </summary>
    /// <param name="light">The light whose button LED to set.</param>
    /// <param name="value">Brightness value between 0.0 and 1.0.</param>
    public void SetButtonLed(Lights light, double value) => _lightManager.SetButtonLed(light, value);

    /// <summary>
    /// Starts non-blocking background monitoring of button presses every 100ms.
    /// Emits the Buttons enum value to ButtonPressedObservable when a button is pressed.
    /// </summary>
    public void StartButtonMonitoring()
    {
        if (_buttonMonitoringTask is { IsCompleted: false })
            return;

        if (_buttonMonitoringCts == null || _buttonMonitoringCts.IsCancellationRequested)
        {
            _buttonMonitoringCts = new CancellationTokenSource();
        }

        _buttonMonitoringTask = Task.Run(async () =>
        {
            var lastPressed = (Buttons?)null;
            while (!_buttonMonitoringCts.Token.IsCancellationRequested)
            {
                try
                {
                    var pressed = Enum.GetValues(typeof(Buttons))
                        .Cast<Buttons?>()
                        .FirstOrDefault(b => b.HasValue && _buttonManager.ReadButton(b.Value));

                    if (pressed != null && lastPressed != pressed)
                    {
                        _buttonPressedObserver.OnNext(pressed);
                        lastPressed = pressed;
                    }
                    else if (pressed == null)
                    {
                        lastPressed = null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Button monitoring error: {ex.Message}");
                }
                finally
                {
                    await Task.Delay(100, _buttonMonitoringCts.Token).ConfigureAwait(false);
                }
            }
        }, _buttonMonitoringCts.Token);
    }

    /// <summary>
    /// Stops the background button monitoring task if running.
    /// </summary>
    public void StopButtonMonitoring()
    {
        if (_buttonMonitoringTask is { IsCompleted: false })
        {
            _buttonMonitoringTask = null;
        }
    }

    #endregion

    #region Motor methods

    // Motor control methods are now delegated to MotorManager

    /// <summary>
    /// Sets the speed and direction of a single motor.
    /// </summary>
    /// <param name="motor">The motor index (0 for left motor, 1 for right).</param>
    /// <param name="speed">Speed value between -1.0 (full reverse) and 1.0 (full forward).</param>
    public void SetMotorSpeed(int motor, double speed) => _motorManager.SetMotorSpeed(motor, speed);

    /// <summary>Sets the speed and direction of both motors.</summary>
    /// <param name="leftSpeed">Speed for the left motor (-1.0 to 1.0).</param>
    /// <param name="rightSpeed">Speed for the right motor (-1.0 to 1.0).</param>
    public void SetMotorSpeeds(double leftSpeed, double rightSpeed) => _motorManager.SetMotorSpeeds(leftSpeed, rightSpeed);

    /// <summary>Disables both motors and sets their PWM to 0.</summary>
    public void DisableMotors() => _motorManager.DisableMotors();

    /// <summary>Drives both motors forward at the specified speed.</summary>
    /// <param name="speed">Speed value (default 0.75).</param>
    public void Forward(double speed = 0.75) => _motorManager.Forward(speed);

    /// <summary>Drives both motors backward at the specified speed.</summary>
    /// <param name="speed">Speed value (default 0.75).</param>
    public void Backward(double speed = 0.75) => _motorManager.Backward(speed);

    /// <summary>Turns the robot left in place at the specified speed.</summary>
    /// <param name="speed">Speed value (default 0.75).</param>
    public void TurnLeft(double speed = 0.75) => _motorManager.TurnLeft(speed);

    /// <summary>Turns the robot right in place at the specified speed.</summary>
    /// <param name="speed">Speed value (default 0.75).</param>
    public void TurnRight(double speed = 0.75) => _motorManager.TurnRight(speed);

    /// <summary>Curves forward left (left motor stopped, right motor forward).</summary>
    /// <param name="speed">Speed value (default 0.75).</param>
    public void CurveForwardLeft(double speed = 0.75) => _motorManager.CurveForwardLeft(speed);

    /// <summary>Curves forward right (right motor stopped, left motor forward).</summary>
    /// <param name="speed">Speed value (default 0.75).</param>
    public void CurveForwardRight(double speed = 0.75) => _motorManager.CurveForwardRight(speed);

    /// <summary>Curves backward left (left motor stopped, right motor backward).</summary>
    /// <param name="speed">Speed value (default 0.75).</param>
    public void CurveBackwardLeft(double speed = 0.75) => _motorManager.CurveBackwardLeft(speed);

    /// <summary>Curves backward right (right motor stopped, left motor backward).</summary>
    /// <param name="speed">Speed value (default 0.75).</param>
    public void CurveBackwardRight(double speed = 0.75) => _motorManager.CurveBackwardRight(speed);

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
    public void SetUnderlight(Lights light, byte r, byte g, byte b, bool show = true) => _lightManager.SetUnderlight(light, r, g, b, show);

    /// <summary>Sets the HSV value of a single underlight.</summary>
    /// <param name="light">The index of the underlight (0-5).</param>
    /// <param name="h">Hue value.</param>
    /// <param name="s">Saturation value.</param>
    /// <param name="v">Value (brightness).</param>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void SetUnderlightHsv(Lights light, double h, double s = 1.0, double v = 1.0, bool show = true) => _lightManager.SetUnderlightHsv(light, h, s, v, show);

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
    public void ClearUnderlight(Lights light, bool show = true) => _lightManager.ClearUnderlight(light, show);

    /// <summary>Clears all underlights (sets them to off).</summary>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void ClearUnderlighting(bool show = true) => _lightManager.ClearUnderlighting(show);

    /// <summary>Sets the RGB value for multiple underlights.</summary>
    /// <param name="lights">Array of underlight indices.</param>
    /// <param name="r">Red value (0-255).</param>
    /// <param name="g">Green value (0-255).</param>
    /// <param name="b">Blue value (0-255).</param>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void SetUnderlights(Lights[] lights, byte r, byte g, byte b, bool show = true) => _lightManager.SetUnderlights(lights, r, g, b, show);

    /// <summary>Sets the HSV value for multiple underlights.</summary>
    /// <param name="lights">Array of underlight indices.</param>
    /// <param name="h">Hue value.</param>
    /// <param name="s">Saturation value.</param>
    /// <param name="v">Value (brightness).</param>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void SetUnderlightsHsv(Lights[] lights, double h, double s = 1.0, double v = 1.0, bool show = true) => _lightManager.SetUnderlightsHsv(lights, h, s, v, show);

    /// <summary>Clears multiple underlights (sets them to off).</summary>
    /// <param name="lights">Array of underlight indices.</param>
    /// <param name="show">Whether to immediately update the lights.</param>
    public void ClearUnderlights(Lights[] lights, bool show = true) => _lightManager.ClearUnderlights(lights, show);

    #endregion

    #region Ultrasound methods

    /// <summary>
    /// Reads the distance using the ultrasonic sensor.
    /// </summary>
    /// <returns>Distance in centimeters</returns>
    public double ReadDistance()
        => _ultrasoundManager.ReadDistance();

    #endregion

    #region Camera
    
    /// <summary>
    /// Takes a photo using the camera and saves it to the specified filename.
    /// This method blocks until the photo is taken.
    /// It returns the full path to the saved photo.
    /// If an error occurs, it returns an empty string.
    /// The filename should include the full path and file extension (e.g., photos/photo.jpg
    /// </summary>
    /// <param name="filename"></param>
    /// <returns>Image data</returns>
    public Task<string> TakePhotoAsync(string filename)
    {
        try
        {
            return _cameraManager.TakePhotoAsync(filename);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error taking photo: {ex.Message}");
            return Task.FromResult(string.Empty);
        }
    }

    #endregion

    #region IDisposable Support

    /// <summary>
    /// Disposes the TriloBot and all its subsystems.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        Console.WriteLine("Disposing TriloBot...");

        try
        {
            StopDistanceMonitoring();
            StopButtonMonitoring();
            DisableUnderlighting();
            DisableMotors();
            _motorManager.Dispose();
            _buttonManager.Dispose();
            _lightManager.Dispose();
            _ultrasoundManager.Dispose();
            _gpio.Dispose();
            _distanceObserver.Dispose();
            _objectTooNearObserver.Dispose();
            _buttonPressedObserver.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping distance monitoring: {ex.Message}");
        }
        finally
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    #endregion
}
