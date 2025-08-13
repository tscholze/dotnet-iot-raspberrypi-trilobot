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
    /// Observable for the connection status.
    /// </summary>
    public IObservable<bool> IsConnectedObservable => _isConnectedObserver.AsObservable();

    /// <summary>
    /// Observable for the connection status.
    /// </summary>
    public IObservable<double> DistanceObservable => _distanceObserver.AsObservable();

    /// <summary>
    /// Observable for the connection status.
    /// </summary>
    public IObservable<bool> ObjectTooNear => _objectToNearObserver.AsObservable();

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
    private readonly BehaviorSubject<bool> _objectToNearObserver = new(false);

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

    #region Private methods

    /// <summary>
    /// Connects to the SignalR Hub.
    /// </summary>
    private async Task StartConnection()
    {
        try
        {
            await hubConnection.StartAsync();
            await StartDistanceUpdates();
            await StartCollisionUpdates();

            _isConnectedObserver.OnNext(true);
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
            hubConnection.On<double>("DistanceUpdate", _distanceObserver.OnNext);
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
            _objectTooNearSubscription = hubConnection.On<bool>("ObjectTooNear", _objectToNearObserver.OnNext);
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
