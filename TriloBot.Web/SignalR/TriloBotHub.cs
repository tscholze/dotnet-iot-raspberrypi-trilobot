using Microsoft.AspNetCore.SignalR;
using TriloBot.Button;
using TriloBot.Light;
using TriloBot.Motor;

namespace TriloBot.Web.SignalR;

/// <summary>
/// SignalR Hub for remote controlling the TriloBot.
/// Exposes key TriloBot methods to web clients.
/// </summary>
public class TriloBotHub : Hub
{
    #region Constants

    /// <summary>
    /// The endpoint for the SignalR hub.
    /// </summary>
    public const string HubEndpoint = "/trilobotHub";

    #endregion

    #region Private Fields

    /// <summary>
    /// Instance of the TriloBot that this hub controls.
    /// </summary>
    private readonly TriloBot _robot;

    /// <summary>
    /// Subscription for distance updates.
    /// </summary>
    private readonly IDisposable? _distanceSubscription;

    /// <summary>
    /// Subscription for object proximity updates.
    /// </summary>
    private readonly IDisposable? _objectTooNearSubscription;

    /// <summary>
    /// Subscription for button press updates.
    /// </summary>
    private readonly IDisposable? _buttonPressSubscription;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="TriloBotHub"/> class and
    /// links it to the specified TriloBot instance.
    /// </summary>
    public TriloBotHub(TriloBot robot)
    {
        // Ensure the robot is not null
        _robot = robot ?? throw new ArgumentNullException(nameof(robot), "TriloBot cannot be null.");
        _robot.StartButtonMonitoring();

        // Handle observers
        _buttonPressSubscription = _robot.ButtonPressedObservable.Subscribe(value =>
        {
            Task.Run(async () => await Clients.All.SendAsync("ButtonPressUpdated", value));
            HandleButtonPress(value);
        });

        _distanceSubscription = robot.DistanceObservable.Subscribe(value =>
        {
            Task.Run(async () => await Clients.All.SendAsync("DistanceUpdated", value));
        });

        _objectTooNearSubscription = robot.ObjectTooNearObservable.Subscribe(value =>
        {
            Task.Run(async () => await Clients.All.SendAsync("ObjectTooNearUpdated", value));
        });
    }

    #endregion

    #region Lights

    /// <summary>
    /// Sets the brightness of a button LED.
    /// </summary>
    /// <param name="lightId">The light id (e.g., 6 for Button A's LED).</param>
    /// <param name="value">Brightness value between 0.0 and 1.0.</param>
    public Task SetButtonLed(int lightId, double value)
        => Task.Run(() => _robot.SetButtonLed((Lights)lightId, value));

    /// <summary>
    /// Fills the underlighting with the specified RGB color.
    /// </summary>
    /// <param name="r">Red value (0-255).</param>
    /// <param name="g">Green value (0-255).</param>
    /// <param name="b">Blue value (0-255).</param>
    public Task FillUnderlighting(byte r, byte g, byte b)
    {
        return Task.Run(() => _robot.FillUnderlighting(r, g, b));
    }

    /// <summary>
    /// Sets the RGB value of a single underlight.
    /// </summary>
    /// <param name="light">The underlight name (e.g., "Light1").</param>
    /// <param name="r">Red value (0-255).</param>
    /// <param name="g">Green value (0-255).</param>
    /// <param name="b">Blue value (0-255).</param>
    public Task SetUnderlight(string light, byte r, byte g, byte b)
        => Task.Run(() => _robot.SetUnderlight(Enum.Parse<Lights>(light), r, g, b));

    public Task ClearUnderlighting()
        => Task.Run(() => _robot.ClearUnderlighting());

    public Task StartPoliceEffect()
        => Task.Run(() => _robot.StartPoliceEffect());

    #endregion

    #region Motors

    /// <summary>
    /// Moves the robot in the specified direction.
    /// </summary>
    /// <param name="horizontal">Horizontal movement (-1 to 1).</param>
    /// <param name="vertical">Vertical movement (-1 to 1).</param>
    public Task Move(double horizontal, double vertical)
        => Task.Run(() => _robot.Move(horizontal, vertical));

    #endregion

    #region Camera

    /// <summary>
    /// Takes a photo using the robot's camera and saves it to the specified path.
    /// </summary>
    /// <param name="savePath">The path to save the photo.</param>
    /// <returns>The full path to the saved photo.</returns>
    public async Task<string> TakePhoto(string savePath)
        => await _robot.TakePhotoAsync(savePath);

    #endregion

    #region Distance

    /// <summary>
    /// Starts monitoring the distance sensor in a background task.
    /// This allows the robot to continuously check for proximity and distance changes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StartDistanceMonitoring()
        => Task.Run(() => _robot.StartDistanceMonitoring());

    /// <summary>
    /// Reads the distance from the ultrasonic sensor.
    /// </summary>
    /// <returns>The distance in centimeters.</returns>
    public Task<double> ReadDistance()
        => Task.FromResult(_robot.ReadDistance());

    #endregion

    #region Buttons

    /// <summary>
    /// Handles button press events to change underlighting colors.
    /// </summary>
    /// <param name="button">Pressed button.</param>
    public void HandleButtonPress(Buttons? button)
    {
        switch (button)
        {
            case Buttons.ButtonA:
                _robot.FillUnderlighting(255, 255, 0);
                break;
            case Buttons.ButtonB:
                _robot.FillUnderlighting(0, 255, 0);
                break;
            case Buttons.ButtonX:
                _robot.FillUnderlighting(0, 0, 255);
                break;
            case Buttons.ButtonY:
                _robot.FillUnderlighting(255, 0, 255);
                break;
            default:
                break;
        }
    }

    #region Life cycle 

    /// <summary>
    /// Closes the hub and disposes of all resources.
    /// </summary>
    public void Close()
    {
        _distanceSubscription?.Dispose();
        _objectTooNearSubscription?.Dispose();
        _robot.Dispose();
    }

    #endregion
}