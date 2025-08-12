using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using TriloBot.Light;

namespace TriloBot.Blazor.SignalR;

/// <summary>
/// SignalR Hub for remote controlling the TriloBot.
/// Exposes key TriloBot methods to web clients.
/// </summary>
public class TriloBotHub(TriloBot robot) : Hub
{
    #region Constants

    /// <summary>
    /// The endpoint for the SignalR hub.
    /// </summary>
    public const string HubEndpoint = "/trilobotHub";

    #endregion

    #region Lights

    /// <summary>
    /// Sets the brightness of a button LED.
    /// </summary>
    /// <param name="lightId">The light id (e.g., 6 for Button A's LED).</param>
    /// <param name="value">Brightness value between 0.0 and 1.0.</param>
    public Task SetButtonLed(int lightId, double value)
        => Task.Run(() => robot.SetButtonLed((Lights)lightId, value));

    /// <summary>
    /// Fills the underlighting with the specified RGB color.
    /// </summary>
    /// <param name="r">Red value (0-255).</param>
    /// <param name="g">Green value (0-255).</param>
    /// <param name="b">Blue value (0-255).</param>
    public Task FillUnderlighting(byte r, byte g, byte b)
        => Task.Run(() => robot.FillUnderlighting(r, g, b));

    /// <summary>
    /// Sets the RGB value of a single underlight.
    /// </summary>
    /// <param name="light">The underlight name (e.g., "Light1").</param>
    /// <param name="r">Red value (0-255).</param>
    /// <param name="g">Green value (0-255).</param>
    /// <param name="b">Blue value (0-255).</param>
    public Task SetUnderlight(string light, byte r, byte g, byte b)
        => Task.Run(() => robot.SetUnderlight(Enum.Parse<Lights>(light), r, g, b));

    #endregion

    #region Motors

    /// <summary>
    /// Moves the robot in the specified direction.
    /// </summary>
    /// <param name="horizontal">Horizontal movement (-1 to 1).</param>
    /// <param name="vertical">Vertical movement (-1 to 1).</param>
    public Task Move(double horizontal, double vertical) => Task.Run(() => robot.Move(horizontal, vertical));

    /// <summary>
    /// Moves the robot forward at the default speed.
    /// </summary>
    public Task Forward() => Task.Run(() => robot.Forward());

    /// <summary>
    /// Moves the robot backward at the default speed.
    /// </summary>
    public Task Backward() => Task.Run(() => robot.Backward());

    /// <summary>
    /// Turns the robot left in place at the default speed.
    /// </summary>
    public Task TurnLeft() => Task.Run(() => robot.TurnLeft());

    /// <summary>
    /// Turns the robot right in place at the default speed.
    /// </summary>
    public Task TurnRight() => Task.Run(() => robot.TurnRight());

    /// <summary>
    /// Stops the robot's motors.
    /// </summary>
    public Task Stop() => Task.Run(robot.Stop);

    #endregion

    #region Camera

    /// <summary>
    /// Takes a photo using the robot's camera and saves it to the specified path.
    /// </summary>
    /// <param name="savePath">The path to save the photo.</param>
    /// <returns>The full path to the saved photo.</returns>
    public async Task<string> TakePhoto(string savePath)
        => await robot.TakePhotoAsync(savePath);

    #endregion

    #region Distance

    /// <summary>
    /// Starts monitoring the distance sensor in a background task.
    /// This allows the robot to continuously check for proximity and distance changes.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task StartDistanceMonitoring()
        => Task.Run(() => robot.StartDistanceMonitoring());

    /// <summary>
    /// Reads the distance from the ultrasonic sensor.
    /// </summary>
    /// <returns>The distance in centimeters.</returns>
    public Task<double> ReadDistance()
        => Task.FromResult(robot.ReadDistance());

    #endregion

    #region RealTimeEvents

    /// <summary>
    /// Streams real-time distance sensor values to clients.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop the stream.</param>
    /// <returns>A channel reader for the distance stream.</returns>
    public ChannelReader<double> DistanceStream(CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<double>();
        var subscription = robot.DistanceObservable
            .Subscribe(async void (value) =>
                {
                    try
                    {
                        await channel.Writer.WriteAsync(value, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        // If the channel is completed or if the writing fails, log the error
                        Console.WriteLine($"Error writing to distance stream: {e.Message}");
                        channel.Writer.TryComplete(e);
                    }
                },
            ex => channel.Writer.TryComplete(ex),
            () => channel.Writer.TryComplete());
        cancellationToken.Register(() =>
        {
            subscription.Dispose();
            channel.Writer.TryComplete();
        });
        return channel.Reader;
    }

    /// <summary>
    /// Streams real-time proximity (object too near) events to clients.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop the stream.</param>
    /// <returns>A channel reader for the proximity stream.</returns>
    public ChannelReader<bool> ObjectTooNearStream(CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<bool>();
        var subscription = robot.ObjectTooNearObservable
            .Subscribe(async void (value) =>
                {
                    try
                    {
                        await channel.Writer.WriteAsync(value, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error writing to channel: {e.Message}");
                        channel.Writer.TryComplete(e);
                    }
                },
            ex => channel.Writer.TryComplete(ex),
            () => channel.Writer.TryComplete());
        cancellationToken.Register(() =>
        {
            subscription.Dispose();
            channel.Writer.TryComplete();
        });
        return channel.Reader;
    }
    
    #endregion
}