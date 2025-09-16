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
using TriloBot.RemoteController;
using TriloBot.Sound;
using TriloBotSystem = TriloBot.SystemInfo;

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
    /// Exposes the CPU usage percentage as an observable (0.0 to 100.0).
    /// </summary>
    public IObservable<double> CpuUsageObservable => _systemManager.CpuUsageObservable;

    /// <summary>
    /// Exposes the memory usage percentage as an observable (0.0 to 100.0).
    /// </summary>
    public IObservable<double> MemoryUsageObservable => _systemManager.MemoryUsageObservable;

    /// <summary>
    /// Exposes the CPU temperature as an observable (in Celsius).
    /// </summary>
    public IObservable<double> CpuTemperatureObservable => _systemManager.CpuTemperatureObservable;

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
    /// Manages remote controller operations.
    /// </summary>
    private readonly RemoteControllerManager _remoteControllerManager;

    /// <summary>
    /// Manages system information and monitoring.
    /// </summary>
    private readonly TriloBotSystem.SystemManager _systemManager;

    /// <summary>
    /// Manages sound operations and audio playback.
    /// </summary>
    private readonly SoundManager _soundManager;

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

    /// <summary>
    /// Subscription for the controller button pressed observable.
    /// </summary>
    private IDisposable? _controllerButtonPressedObservable;

    /// <summary>
    /// Subscription for the controller horizontal movement observable.
    /// </summary>
    private IDisposable? _controllerHorizontalMovementObservable;

    /// <summary>
    /// Subscription for the controller vertical movement observable.
    /// </summary>
    private IDisposable? _controllerVerticalMovementObservable;

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

        // Setup system manager
        _systemManager = new TriloBotSystem.SystemManager();

        // Setup sound manager
        _soundManager = new SoundManager();

        // Setup remote controller manager - defaulting to Xbox Series for compatibility
        _remoteControllerManager = new RemoteControllerManager(ControllerType.XboxSeries);
        _controllerVerticalMovementObservable = _remoteControllerManager.VerticalMovementObservable.Subscribe(OnVerticalMovementChanged);
        _controllerHorizontalMovementObservable = _remoteControllerManager.HorizontalMovementObservable.Subscribe(OnHorizontalMovementChanged);
        _controllerButtonPressedObservable = _remoteControllerManager.ButtonPressedObservable.Subscribe(OnControllerButtonPressed);

        // Register cancellation token to handle graceful shutdown
        // This allows the TriloBot to clean up resources and stop ongoing operations when cancellation is requested
        // It ensures that any ongoing effects or operations are properly terminated
        cancellationToken.Register(() =>
        {
            // Cancel any ongoing effects or operations
            _lightManager.DisableUnderlighting();
            StopDistanceMonitoring();
            Move(0, 0);

            // Caution:
            // Do not call Dispose() here; handled by using statement
        });

        _soundManager.PlayHorn();

        // Log successful initialization
        Console.WriteLine("Successfully initialized TriloBot manager. Start observer listing or triggering other methods.");
    }

    /// <summary>
    /// Handles button press events from the remote controller.
    /// </summary>
    private void OnControllerButtonPressed(Buttons? buttons)
    {
        switch (buttons)
        {
            case Buttons.ButtonA:
                _lightManager.FillUnderlighting(255, 0, 0);
                break;
            case Buttons.ButtonB:
                _lightManager.FillUnderlighting(0, 0, 0);
                break;
            case Buttons.ButtonX:
                Console.WriteLine("Button X pressed");
                break;
            case Buttons.ButtonY:
                Console.WriteLine("Button Y pressed");
                break;
            default:
                Console.WriteLine("Unknown button pressed");
                break;
        }
    }

    private void OnHorizontalMovementChanged(double horizontal)
    {
        var vertical = _remoteControllerManager.VerticalMovementObservable.Latest().FirstOrDefault();
        Move(horizontal, vertical);
    }

    private void OnVerticalMovementChanged(double vertical)
    {
        var horizontal = _remoteControllerManager.HorizontalMovementObservable.Latest().FirstOrDefault();
        Move(horizontal, vertical);
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

    /// <summary>
    /// Controls the robot's movement using normalized horizontal and vertical values.
    /// </summary>
    /// <param name="horizontal">Horizontal movement: -1 (left) to 1 (right), 0 = no turn</param>
    /// <param name="vertical">Vertical movement: -1 (backward) to 1 (forward), 0 = stop</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when horizontal or vertical values are outside the range [-1, 1]</exception>
    public void Move(double horizontal, double vertical)
    {
        // Validate input ranges
        if (Math.Abs(horizontal) > 1.0 || Math.Abs(vertical) > 1.0)
        {
            Move(0, 0);
            throw new ArgumentOutOfRangeException(nameof(horizontal), "Value must be between -1 and 1.");
        }

        // Stop if movement is below threshold
        if (Math.Abs(vertical) < MovementChangedThreshold && Math.Abs(horizontal) < MovementChangedThreshold)
        {
            _motorManager.SetMotorSpeeds(0, 0);
            return;
        }

        // Handle pure rotation (turn in place)
        if (Math.Abs(vertical) < MovementChangedThreshold)
        {
            _motorManager.SetMotorSpeeds(-horizontal, horizontal);
            return;
        }

        // Calculate differential steering for smooth turning
        var baseSpeed = Math.Abs(vertical);
        var turnFactor = Math.Abs(horizontal);

        var leftSpeed = horizontal < 0 ? baseSpeed * (1.0 - turnFactor) : baseSpeed;
        var rightSpeed = horizontal > 0 ? baseSpeed * (1.0 - turnFactor) : baseSpeed;

        // Apply direction (forward/backward)
        if (vertical < 0)
        {
            leftSpeed = -leftSpeed;
            rightSpeed = -rightSpeed;
        }

        _motorManager.SetMotorSpeeds(leftSpeed, rightSpeed);
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

    #region System methods

    /// <summary>
    /// Gets the system hostname.
    /// </summary>
    /// <returns>The system hostname.</returns>
    public string GetHostname() => _systemManager.Hostname;

    /// <summary>
    /// Gets the primary IP address of the system.
    /// </summary>
    /// <returns>The primary IP address.</returns>
    public string GetPrimaryIpAddress() => _systemManager.GetPrimaryIpAddress();

    /// <summary>
    /// Gets all network interfaces and their IP addresses.
    /// </summary>
    /// <returns>A dictionary mapping interface names to their IP addresses.</returns>
    public Dictionary<string, List<string>> GetNetworkInterfaces() => _systemManager.GetNetworkInterfaces();

    /// <summary>
    /// Gets CPU information.
    /// </summary>
    /// <returns>A dictionary containing CPU information.</returns>
    public Dictionary<string, string> GetCpuInfo() => _systemManager.GetCpuInfo();

    /// <summary>
    /// Gets memory information.
    /// </summary>
    /// <returns>A dictionary containing memory information in KB.</returns>
    public Dictionary<string, long> GetMemoryInfo() => _systemManager.GetMemoryInfo();

    /// <summary>
    /// Gets the current CPU temperature.
    /// </summary>
    /// <returns>CPU temperature in Celsius.</returns>
    public double GetCpuTemperature() => _systemManager.GetCpuTemperature();

    /// <summary>
    /// Gets the system load averages.
    /// </summary>
    /// <returns>A tuple containing (load1min, load5min, load15min).</returns>
    public (double load1min, double load5min, double load15min) GetLoadAverages() => _systemManager.GetLoadAverages();

    /// <summary>
    /// Gets the system uptime.
    /// </summary>
    /// <returns>System uptime as a TimeSpan.</returns>
    public TimeSpan GetSystemUptime() => _systemManager.SystemUptime;

    /// <summary>
    /// Starts system monitoring for CPU usage, memory usage, and CPU temperature.
    /// </summary>
    /// <param name="intervalMs">Monitoring interval in milliseconds.</param>
    public void StartSystemMonitoring(int intervalMs = 2000) => _systemManager.StartMonitoring(intervalMs);

    /// <summary>
    /// Stops system monitoring.
    /// </summary>
    public void StopSystemMonitoring() => _systemManager.StopMonitoring();

    #endregion

    #region Sound methods

    /// <summary>
    /// Plays the horn sound effect asynchronously.
    /// This is a convenience method for the most common robot sound.
    /// </summary>
    /// <returns>A task representing the asynchronous sound playback operation.</returns>
    /// <exception cref="FileNotFoundException">Thrown when horn.wav file is not found in the sound directory.</exception>
    public async Task PlayHornAsync()
    {
        try
        {
            await _soundManager.PlayHornAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing horn sound: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Plays the horn sound effect synchronously.
    /// This method blocks until playback is complete.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown when horn.wav file is not found in the sound directory.</exception>
    public void PlayHorn()
    {
        try
        {
            _soundManager.PlayHorn();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing horn sound: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Plays a specified sound file asynchronously using the default volume.
    /// </summary>
    /// <param name="fileName">The name of the sound file to play (with extension).</param>
    /// <returns>A task representing the asynchronous sound playback operation.</returns>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified sound file is not found.</exception>
    public async Task PlaySoundAsync(string fileName)
    {
        try
        {
            await _soundManager.PlaySoundAsync(fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing sound '{fileName}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Plays a specified sound file asynchronously with a custom volume level.
    /// </summary>
    /// <param name="fileName">The name of the sound file to play (with extension).</param>
    /// <param name="volume">Volume level between 0.0 (mute) and 1.0 (maximum volume).</param>
    /// <returns>A task representing the asynchronous sound playback operation.</returns>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is outside the range [0.0, 1.0].</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified sound file is not found.</exception>
    public async Task PlaySoundAsync(string fileName, double volume)
    {
        try
        {
            await _soundManager.PlaySoundAsync(fileName, volume);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing sound '{fileName}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Plays a specified sound file synchronously using the default volume.
    /// This method blocks until playback is complete.
    /// </summary>
    /// <param name="fileName">The name of the sound file to play (with extension).</param>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified sound file is not found.</exception>
    public void PlaySound(string fileName)
    {
        try
        {
            _soundManager.PlaySound(fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing sound '{fileName}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Plays a specified sound file synchronously with a custom volume level.
    /// This method blocks until playback is complete.
    /// </summary>
    /// <param name="fileName">The name of the sound file to play (with extension).</param>
    /// <param name="volume">Volume level between 0.0 (mute) and 1.0 (maximum volume).</param>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is outside the range [0.0, 1.0].</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified sound file is not found.</exception>
    public void PlaySound(string fileName, double volume)
    {
        try
        {
            _soundManager.PlaySound(fileName, volume);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing sound '{fileName}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Checks if a specified sound file exists in the sound directory.
    /// </summary>
    /// <param name="fileName">The name of the sound file to check (with extension).</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty.</exception>
    public bool SoundFileExists(string fileName)
    {
        return _soundManager.SoundFileExists(fileName);
    }

    /// <summary>
    /// Gets a list of all available sound files in the sound directory.
    /// </summary>
    /// <returns>An array of sound file names (with extensions).</returns>
    public string[] GetAvailableSoundFiles()
    {
        return _soundManager.GetAvailableSoundFiles();
    }

    /// <summary>
    /// Sets the system volume level (master volume).
    /// This affects the overall system audio output.
    /// </summary>
    /// <param name="volume">Volume level between 0.0 (mute) and 1.0 (maximum volume).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is outside the range [0.0, 1.0].</exception>
    public async Task SetSystemVolumeAsync(double volume)
    {
        try
        {
            await _soundManager.SetSystemVolumeAsync(volume);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting system volume: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets or sets the default volume level for sound playback.
    /// </summary>
    /// <value>Volume level between 0.0 (mute) and 1.0 (maximum volume).</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is outside the range [0.0, 1.0].</exception>
    public double DefaultSoundVolume
    {
        get => _soundManager.DefaultVolume;
        set => _soundManager.DefaultVolume = value;
    }

    /// <summary>
    /// Gets the current sound directory path where sound files are stored.
    /// </summary>
    public string SoundDirectory => _soundManager.SoundDirectory;

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
            _controllerButtonPressedObservable = null;
            _controllerHorizontalMovementObservable = null;
            _controllerVerticalMovementObservable = null;
            _motorManager.Dispose();
            _buttonManager.Dispose();
            _lightManager.Dispose();
            _ultrasoundManager.Dispose();
            _systemManager.Dispose();
            _soundManager.Dispose();
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
