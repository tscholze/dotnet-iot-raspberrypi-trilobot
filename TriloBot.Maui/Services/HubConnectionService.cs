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
    /// Gets a value indicating whether the SignalR connection is established.
    /// </summary>
    public bool IsConnected => _hubConnection.State == HubConnectionState.Connected;

    /// <summary>
    /// Emits true if the SignalR connection is established, false otherwise.
    /// </summary>
    public IObservable<bool> IsConnectedObservable => _isConnectedObserver.AsObservable();

    /// <summary>
    /// Emits real-time distance sensor values from the robot (in centimeters).
    /// </summary>
    public IObservable<double> DistanceObservable => _distanceObserver.AsObservable();

    /// <summary>
    /// Emits real-time proximity alerts (true if an object is detected too nearby the robot's sensor).
    /// </summary>
    public IObservable<bool> ObjectTooNearObservable => _objectTooNearObserver.AsObservable();

    #endregion

    #region Private Properties

    /// <summary>
    /// Hub connection service for managing SignalR connections.
    /// </summary>  
    private readonly HubConnection _hubConnection;

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
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://pi5:6969/trilobotHub") // Replace with your server address if needed
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.Closed += (error) =>
        {
            Application.Current?.Dispatcher.Dispatch(() => _isConnectedObserver.OnNext(false));
            Console.WriteLine("Connection closed with error: " + (error?.Message ?? "No error"));
            return Task.CompletedTask;
        };
        
        _hubConnection.Reconnecting += (error) =>
        {
            Application.Current?.Dispatcher.Dispatch(() => _isConnectedObserver.OnNext(false));
            Console.WriteLine("Reconnecting to SignalR Hub...");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += (connectionId) =>
        {
            Application.Current?.Dispatcher.Dispatch(() => _isConnectedObserver.OnNext(true));
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
            await _hubConnection.InvokeAsync("Move", horizontal, vertical);
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
            await _hubConnection.InvokeAsync(methodName, args);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error invoking {methodName}: {e.Message}");
        }
    }
    
    /// <summary>
    /// Switches the robot's lights on with white color.
    /// </summary>
    public async Task LightsOn()
    {
        await SafeInvokeAsync("FillUnderlighting", 255, 255, 255);
    }
    
    /// <summary>
    /// Switches the robot's lights off.
    /// </summary>
    public async Task LightsOff()
    {
        await SafeInvokeAsync("ClearUnderlighting");
    }
    
    /// <summary>
    /// Plays police lights effect on the robot.
    /// This method is used to start the special lights effect, such as police lights.
    /// </summary>
    public async Task StartSpecialLights()
    {
        await SafeInvokeAsync("StartPoliceEffect");
    }
    
    public async Task ToggleAutopilot()
    {
        await SafeInvokeAsync("StartAutopilot");
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
            await _hubConnection.StartAsync();
            await StartDistanceUpdates();

            Application.Current?.Dispatcher.Dispatch(() => _isConnectedObserver.OnNext(_hubConnection.State == HubConnectionState.Connected));
            Console.WriteLine("Connected to SignalR Hub");
        }
        catch (Exception ex)
        {
            Application.Current?.Dispatcher.Dispatch(() => _isConnectedObserver.OnNext(false));
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
        Console.WriteLine("Subscribing to distance updates...");
        
        try
        {
            // Subscribe to value updates from the hub
            _distanceSubscription = _hubConnection.On<double>(
                "DistanceUpdated",
                d => Application.Current?.Dispatcher.Dispatch(() => _distanceObserver.OnNext(d))
            );
            
            _objectTooNearSubscription = _hubConnection.On<bool>(
                "ObjectTooNearUpdated",
                b => Application.Current?.Dispatcher.Dispatch(() => _objectTooNearObserver.OnNext(b))
            );

            // Ensure the robot starts pushing distance updates
            await _hubConnection.InvokeAsync("StartDistanceMonitoring");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error subscribing to distance updates: {ex.Message}");
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
        Task.Run(async () => await _hubConnection.DisposeAsync());
        GC.SuppressFinalize(this);
    }

    #endregion
}
