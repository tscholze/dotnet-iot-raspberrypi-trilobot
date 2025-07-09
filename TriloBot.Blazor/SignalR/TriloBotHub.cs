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
    public const string HubEndpoint = "/trilobotHub";

    #region Button_LED_Light
    public Task SetButtonLed(string button, double value)
        => Task.Run(() => _robot.SetButtonLed(Enum.Parse<Buttons>(button), value));

    public Task FillUnderlighting(byte r, byte g, byte b)
        => Task.Run(() => _robot.FillUnderlighting(r, g, b));

    public Task SetUnderlight(string light, byte r, byte g, byte b)
        => Task.Run(() => _robot.SetUnderlight(Enum.Parse<Lights>(light), r, g, b));

    #endregion

    #region Motor
    public Task Forward(double speed = 0.25) => Task.Run(() => _robot.Forward(speed));
    public Task Backward(double speed = 0.25) => Task.Run(() => _robot.Backward(speed));
    public Task TurnLeft(double speed = 0.25) => Task.Run(() => _robot.TurnLeft(speed));
    public Task TurnRight(double speed = 0.25) => Task.Run(() => _robot.TurnRight(speed));
    public Task Stop() => Task.Run(() => _robot.Stop());

    #endregion

    #region Camera
    public async Task<string> TakePhoto(string savePath)
        => await _robot.TakePhotoAsync(savePath);

    public Task<string> GetLiveStreamUrl()
        => Task.FromResult(_robot.GetLiveStreamUrl());

    #endregion

    #region Distance
    public Task<double> ReadDistance()
        => Task.FromResult(_robot.ReadDistance());

    #endregion

    #region RealTimeEvents
    /// <summary>
    /// Streams real-time distance sensor values to clients.
    /// </summary>
    public ChannelReader<double> DistanceStream(CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<double>();
        var subscription = _robot.DistanceObservable
            .Subscribe(async value => {
                await channel.Writer.WriteAsync(value, cancellationToken);
            },
            ex => channel.Writer.TryComplete(ex),
            () => channel.Writer.TryComplete());
        cancellationToken.Register(() => {
            subscription.Dispose();
            channel.Writer.TryComplete();
        });
        return channel.Reader;
    }

    /// <summary>
    /// Streams real-time proximity (object too near) events to clients.
    /// </summary>
    public ChannelReader<bool> ObjectTooNearStream(CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<bool>();
        var subscription = _robot.ObjectTooNearObservable
            .Subscribe(async value => {
                await channel.Writer.WriteAsync(value, cancellationToken);
            },
            ex => channel.Writer.TryComplete(ex),
            () => channel.Writer.TryComplete());
        cancellationToken.Register(() => {
            subscription.Dispose();
            channel.Writer.TryComplete();
        });
        return channel.Reader;
    }

    /// <summary>
    /// Streams live video feed URLs to clients (pushes new URL when stream changes).
    /// </summary>
    public ChannelReader<string> LiveVideoFeedStream(CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<string>();
        var subscription = _robot.LiveVideoFeedObservable
            .Subscribe(async url => {
                await channel.Writer.WriteAsync(url, cancellationToken);
            },
            ex => channel.Writer.TryComplete(ex),
            () => channel.Writer.TryComplete());
        cancellationToken.Register(() => {
            subscription.Dispose();
            channel.Writer.TryComplete();
        });
        return channel.Reader;
    }
    #endregion
}