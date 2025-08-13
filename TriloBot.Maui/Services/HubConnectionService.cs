using Microsoft.AspNetCore.SignalR.Client;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace TriloBot.Maui.Services;

/// <summary>
/// Provides a singleton SignalR HubConnection for the app.
/// </summary>
public class HubConnectionService : IDisposable
{
    #region Public Properties

    /// <summary>
    /// Emits true if the SignalR connection is established, false otherwise.
    /// </summary>
    public IObservable<bool> IsConnectedObservable => _isConnectedObserver.AsObservable();

    /// <summary>
    /// Emits real-time distance sensor values from the robot (in centimeters).
    /// </summary>
    public IObservable<double> DistanceObservable => _distanceObserver.AsObservable();

    /// <summary>
    /// Emits real-time proximity alerts (true if an object is detected too near by the robot's sensor).
    /// </summary>
    public IObservable<bool> ObjectTooNearObservable => _objectTooNearObserver.AsObservable();

    #endregion

    #region Private Properties

    /// <summary>
    /// Hub connection service for managing SignalR connections.
    /// </summary>  
    private readonly HubConnection hubConnection;

    /// <summary>
    /// Observable for the latest distance readings.
    /// </summary>
    private readonly BehaviorSubject<double> _distanceObserver = new(0);

    /// <summary>
    /// Observable for the connection status.
    /// </summary>
    private readonly BehaviorSubject<bool> _isConnectedObserver = new(false);

    /// <summary>
    /// Observable for the latest object proximity readings.
    /// </summary>
    private readonly BehaviorSubject<bool> _objectTooNearObserver = new(false);

    /// <summary>
    /// Subscription for distance updates.
    /// </summary>
    private IDisposable? _distanceSubscription;

    /// <summary>
    /// Subscription for object proximity updates.
    /// </summary>
    private IDisposable? _objectTooNearSubscription;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="HubConnectionService"/> class.
    /// </summary>
    public HubConnectionService()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl("http://pi5:6969/trilobotHub") // Replace with your server address if needed
            .WithAutomaticReconnect()
            .Build();

        hubConnection.Closed += (error) =>
        {
            _isConnectedObserver.OnNext(false);
            Console.WriteLine("Connection closed. Waiting for automatic reconnect...");
            return Task.CompletedTask;
        };

        hubConnection.Reconnected += (connectionId) =>
        {
            _isConnectedObserver.OnNext(true);
            Console.WriteLine($"Reconnected to SignalR Hub with connection ID: {connectionId}");
            return Task.CompletedTask;
        };

        Task.Run(StartConnection);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Invokes the Move method on the SignalR hub.
    /// </summary>
    /// <param name="horizontal">The horizontal axis value.</param>
    /// <param name="vertical">The vertical axis value.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeMove(double horizontal, double vertical)
    {
        try
        {
            await hubConnection.InvokeAsync("Move", horizontal, vertical);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error sending joystick movement: {e.Message}");
        }
    }

    /// <summary>
    /// Helper method to safely invoke a SignalR hub method with error handling.
    /// </summary>
    /// <param name="methodName">The name of the hub method to invoke.</param>
    /// <param name="args">Optional arguments to pass to the hub method.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SafeInvokeAsync(string methodName, params object[] args)
    {
        try
        {
            await hubConnection.InvokeAsync(methodName, args);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error invoking {methodName}: {e.Message}");
        }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Connects to the SignalR Hub.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task StartConnection()
    {
        try
        {
            await hubConnection.StartAsync();
            await StartDistanceUpdates();
            await StartCollisionUpdates();

            _isConnectedObserver.OnNext(hubConnection.State == HubConnectionState.Connected);
            Console.WriteLine("Connected to SignalR Hub");
        }
        catch (Exception ex)
        {
            _isConnectedObserver.OnNext(false);
            Console.WriteLine($"Error connecting to SignalR Hub: {ex.Message}");
        }
    }

    #endregion

    #region Observers

    /// <summary>
    /// Starts receiving distance updates from the SignalR hub.
    /// </summary>
    private async Task StartDistanceUpdates()
    {
        try
        {
            _distanceSubscription =hubConnection.On<double>("DistanceUpdate", _distanceObserver.OnNext);
            await hubConnection.InvokeAsync("StartDistanceMonitoring");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error subscribing to distance updates: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts receiving collision warning updates from the SignalR hub.
    /// </summary>
    private async Task StartCollisionUpdates()
    {
        try
        {
            await hubConnection.InvokeAsync("StartCollisionMonitoring");
            _objectTooNearSubscription = hubConnection.On<bool>("ObjectTooNear", _objectTooNearObserver.OnNext);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error subscribing to collision updates: {ex.Message}");
        }
    }

    #endregion

    #region IDisposable implementation

    /// <summary>
    /// Disposes the resources used by the <see cref="HubConnectionService"/>.
    /// </summary>
    public void Dispose()
    {
        _distanceSubscription?.Dispose();
        _objectTooNearSubscription?.Dispose();
        Task.Run(async () => await hubConnection.DisposeAsync());
        GC.SuppressFinalize(this);
    }

    #endregion
}
