using System.Device.Gpio;
using System.Runtime.InteropServices;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using TriloBot.Light;
using TriloBot.Motor;
using TriloBot.Ultrasound;
using TriloBot.Button;
using TriloBot.Camera;
using TriloBot.Light.Modes;

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

    #region Private constants

    /// <summary>
    /// The default speed for the robot.
    /// </summary>
    private const double DefaultSpeed = 0.5;

    /// <summary>
    /// The default critical distance for movements of the robot.
    /// </summary>
    private const double DefaultCriticalDistance = 30.0;

    /// <summary>
    /// The minimum movement threshold for the robot.
    /// </summary>
    private const double MovementChangedThreshold = 0.1;

    /// <summary>
    /// The minimum change in distance required to trigger an update.
    /// </summary>
    private const double DistanceChangeThreshold = 1.0;

    /// <summary>
    /// The default interval for sensor polling.
    /// </summary>
    private const int DefaultSensorPollingInterval = 250;

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

        // Setup camera manager
        _cameraManager = new CameraManager();

        // Register cancellation token to handle graceful shutdown
        // This allows the TriloBot to clean up resources and stop ongoing operations when cancellation is requested
        // It ensures that any ongoing effects or operations are properly terminated
        cancellationToken.Register(() =>
        {
            // Cancel any ongoing effects or operations
            _lightManager.DisableUnderlighting();
            StopDistanceMonitoring();
            Stop();

            // Caution:
            // Do not call Dispose() here; handled by using statement
        });

        // Log successful initialization
        Console.WriteLine("Successfully initialized TriloBot manager. Start observer listing or triggering other methods.");
    }

    #endregion

    #region Distance Monitoring

    /// <summary>
    /// Starts non-blocking background monitoring of the distance sensor.
    /// </summary>
    public void StartDistanceMonitoring(double minDistance = DefaultCriticalDistance)
    {
        if (minDistance <= 0)
            throw new ArgumentOutOfRangeException(nameof(minDistance), "Minimum distance must be greater than zero.");

        if (_distanceMonitoringTask is { IsCompleted: false })
            return;

        _distanceMonitoringCts = new CancellationTokenSource();

        _distanceMonitoringTask = Task.Run(async () =>
        {
            while (!_distanceMonitoringCts.Token.IsCancellationRequested)
            {
                try
                {
                    var distance = _ultrasoundManager.ReadDistance();
                    var isTooNear = distance < minDistance;

                    if (Math.Abs(_distanceObserver.Value - distance) > DistanceChangeThreshold)
                        _distanceObserver.OnNext(distance);

                    if (_objectTooNearObserver.Value != isTooNear)
                        _objectTooNearObserver.OnNext(isTooNear);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Distance monitoring error: {ex.Message}");
                }

                await Task.Delay(DefaultSensorPollingInterval, _distanceMonitoringCts.Token);
            }
        });
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

    #region Button methods

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

        _buttonMonitoringCts ??= new CancellationTokenSource();

        _buttonMonitoringTask = Task.Run(async () =>
        {
            Buttons? lastPressed = null;

            while (!_buttonMonitoringCts.Token.IsCancellationRequested)
            {
                var pressed = Enum.GetValues<Buttons>().FirstOrDefault(_buttonManager.ReadButton);

                if (pressed != lastPressed)
                {
                    _buttonPressedObserver.OnNext(pressed);
                    lastPressed = pressed;
                }

                await Task.Delay(DefaultSensorPollingInterval, _buttonMonitoringCts.Token);
            }
        });
    }

    /// <summary>
    /// Stops the background button monitoring task if running.
    /// </summary>
    public void StopButtonMonitoring()
    {
        if (_buttonMonitoringCts != null)
        {
            _buttonMonitoringCts.Cancel();

            try
            {
                _buttonMonitoringTask?.Wait(1000);
            }
            catch (AggregateException) { }
            catch (OperationCanceledException) { }
            finally
            {
                _buttonMonitoringCts.Dispose();
                _buttonMonitoringCts = null;
                _buttonMonitoringTask = null;
            }
        }
    }

    #endregion

    #region Motor methods

    /// <summary>Sets the speed and direction of both motors.</summary>
    /// <param name="leftSpeed">Speed for the left motor (-1.0 to 1.0).</param>
    /// <param name="rightSpeed">Speed for the right motor (-1.0 to 1.0).</param>
    public void SetMotorSpeeds(double leftSpeed, double rightSpeed) => _motorManager.SetMotorSpeeds(leftSpeed, rightSpeed);

    /// <summary>Drives both motors forward at the specified speed.</summary>
    /// <param name="speed">Speed value.</param>
    public void Forward(double speed = DefaultSpeed) => _motorManager.Forward(speed);

    /// <summary>Drives both motors backward at the specified speed.</summary>
    /// <param name="speed">Speed value.</param>
    public void Backward(double speed = DefaultSpeed) => _motorManager.Backward(speed);

    /// <summary>Turns the robot left in place at the specified speed.</summary>
    /// <param name="speed">Speed value.</param>
    public void TurnLeft(double speed = DefaultSpeed) => _motorManager.TurnLeft(speed);

    /// <summary>Turns the robot right in place at the specified speed.</summary>
    /// <param name="speed">Speed value.</param>
    public void TurnRight(double speed = DefaultSpeed) => _motorManager.TurnRight(speed);

    /// <summary>Curves forward left (left motor stopped, right motor forward).</summary>
    /// <param name="speed">Speed value.</param>
    public void CurveForwardLeft(double speed = DefaultSpeed) => _motorManager.CurveForwardLeft(speed);

    /// <summary>Curves forward right (right motor stopped, left motor forward).</summary>
    /// <param name="speed">Speed value.</param>
    public void CurveForwardRight(double speed = DefaultSpeed) => _motorManager.CurveForwardRight(speed);

    /// <summary>Curves backward left (left motor stopped, right motor backward).</summary>
    /// <param name="speed">Speed value.</param>
    public void CurveBackwardLeft(double speed = DefaultSpeed) => _motorManager.CurveBackwardLeft(speed);

    /// <summary>Curves backward right (right motor stopped, left motor backward).</summary>
    /// <param name="speed">Speed value.</param>
    public void CurveBackwardRight(double speed = DefaultSpeed) => _motorManager.CurveBackwardRight(speed);

    /// <summary>Stops both motors (brake mode).</summary>
    public void Stop() => _motorManager.Stop();

    /// <summary>
    /// Controls the robot's movement using normalized horizontal and vertical values.
    /// Horizontal: -1 (left) to 1 (right), 0 = no turn.
    /// Vertical: -1 (backward) to 1 (forward), 0 = stop.
    /// </summary>
    /// <param name="horizontal">-1 (left) to 1 (right)</param>
    /// <param name="vertical">-1 (backward) to 1 (forward)</param>
    public void Move(double horizontal, double vertical)
    {
        // Step 1: Get absolute values for easier range checks and calculations
        var horizontalAbs = Math.Abs(horizontal);
        var verticalAbs = Math.Abs(vertical);

        // Step 2: Validate horizontal input range
        // If horizontal is outside [-1, 1], stop and throw exception
        if (horizontalAbs > 1)
        {
            Stop();
            throw new ArgumentOutOfRangeException(nameof(horizontal), "Value must be between -1 and 1.");
        }

        // Step 3: Validate vertical input range
        // If vertical is outside [-1, 1], stop and throw exception
        if (verticalAbs > 1)
        {
            Stop();
            throw new ArgumentOutOfRangeException(nameof(vertical), "Value must be between -1 and 1.");
        }

        // Step 4: If vertical is below a movement threshold, stop motors and exit
        if (verticalAbs < MovementChangedThreshold && horizontalAbs < MovementChangedThreshold)
        {
            Console.WriteLine("Stopping motors due to low movement threshold.");
            Stop();
            return;
        }

        Console.WriteLine($"Move: horizontal={horizontal}, vertical={vertical}");


        // Step 5: Calculate base speed for both motors
        double leftSpeed = verticalAbs;
        double rightSpeed = verticalAbs;

        // Step 6: Apply turning logic
        // Reduce speed on one side for turning
        // If horizontal is negative, turn left by reducing left motor speed
        // If horizontal is positive, turn right by reducing right motor speed
        if (horizontal < 0)
        {
            leftSpeed *= 1.0 - Math.Abs(horizontal);
        }
        else
        {
            rightSpeed *= 1.0 - Math.Abs(horizontal);
        }

        // Step 7: Apply direction
        // If vertical is positive, move forward
        // If vertical is negative, move backward
        // Vertical 0 is required to only have sharp turns as a feature.

Console.WriteLine($"Calculated speeds: left={leftSpeed}, right={rightSpeed} for vertical={vertical}");
        if (vertical >= 0)
        {
            SetMotorSpeeds(leftSpeed, rightSpeed);
        }
        else
        {
            SetMotorSpeeds(-leftSpeed, -rightSpeed);
        }
    }

    #endregion

    #region Light methods

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

    /// <summary>
    /// Starts the police lights effect, which alternates red and blue lights.
    /// This method does not block and can be called multiple times to restart the effect.
    /// </summary>
    public void StartPoliceEffect()
        => _lightManager.PoliceLightsEffect();

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
