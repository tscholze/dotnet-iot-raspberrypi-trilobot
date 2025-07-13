using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using TriloBot.Light;
using TriloBot.Button;

namespace TriloBot.Blazor.SignalR;

/// <summary>
/// SignalR Hub for remote controlling the TriloBot.
/// Exposes key TriloBot methods to web clients.
/// </summary>
public class TriloBotHub(TriloBot _robot) : Hub
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
    /// <param name="button">The button name (e.g., "ButtonA").</param>
    /// <param name="value">Brightness value between 0.0 and 1.0.</param>
    public Task SetButtonLed(string button, double value)
        => Task.Run(() => _robot.SetButtonLed(Enum.Parse<Buttons>(button), value));

    /// <summary>
    /// Fills the underlighting with the specified RGB color.
    /// </summary>
    /// <param name="r">Red value (0-255).</param>
    /// <param name="g">Green value (0-255).</param>
    /// <param name="b">Blue value (0-255).</param>
    public Task FillUnderlighting(byte r, byte g, byte b)
        => Task.Run(() => _robot.FillUnderlighting(r, g, b));

    /// <summary>
    /// Sets the RGB value of a single underlight.
    /// </summary>
    /// <param name="light">The underlight name (e.g., "Light1").</param>
    /// <param name="r">Red value (0-255).</param>
    /// <param name="g">Green value (0-255).</param>
    /// <param name="b">Blue value (0-255).</param>
    public Task SetUnderlight(string light, byte r, byte g, byte b)
        => Task.Run(() => _robot.SetUnderlight(Enum.Parse<Lights>(light), r, g, b));

    #endregion

    #region Motors

    /// <summary>
    /// Moves the robot forward at the default speed.
    /// </summary>
    public Task Forward() => Task.Run(() => _robot.Forward());

    /// <summary>
    /// Moves the robot backward at the default speed.
    /// </summary>
    public Task Backward() => Task.Run(() => _robot.Backward());

    /// <summary>
    /// Turns the robot left in place at the default speed.
    /// </summary>
    public Task TurnLeft() => Task.Run(() => _robot.TurnLeft());

    /// <summary>
    /// Turns the robot right in place at the default speed.
    /// </summary>
    public Task TurnRight() => Task.Run(() => _robot.TurnRight());

    /// <summary>
    /// Stops the robot's motors.
    /// </summary>
    public Task Stop() => Task.Run(() => _robot.Stop());

    #endregion

    #region Camera

    /// <summary>
    /// Takes a photo using the robot's camera and saves it to the specified path.
    /// </summary>
    /// <param name="savePath">The path to save the photo.</param>
    /// <returns>The full path to the saved photo.</returns>
    public async Task<string> TakePhoto(string savePath)
        => await _robot.TakePhotoAsync(savePath);

    /// <summary>
    /// Gets the URL for the live video stream from the robot's camera.
    /// </summary>
    /// <returns>The live stream URL.</returns>
    public Task<string> GetLiveStreamUrl()
        => Task.FromResult(_robot.GetLiveStreamUrl());

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

    #region RealTimeEvents

    /// <summary>
    /// Streams real-time distance sensor values to clients.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop the stream.</param>
    /// <returns>A channel reader for the distance stream.</returns>
    public ChannelReader<double> DistanceStream(CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<double>();
        var subscription = _robot.DistanceObservable
            .Subscribe(async value =>
            {
                await channel.Writer.WriteAsync(value, cancellationToken);
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
        var subscription = _robot.ObjectTooNearObservable
            .Subscribe(async value =>
            {
                await channel.Writer.WriteAsync(value, cancellationToken);
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
    /// Streams live video feed URLs to clients (pushes new URL when stream changes).
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to stop the stream.</param>
    /// <returns>A channel reader for the live video feed stream.</returns>
    public ChannelReader<string> LiveVideoFeedStream(CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<string>();
        var subscription = _robot.LiveVideoFeedObservable
            .Subscribe(async url =>
            {
                await channel.Writer.WriteAsync(url, cancellationToken);
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