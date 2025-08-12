using Microsoft.AspNetCore.SignalR.Client;

namespace TriloBot.Maui.Pages;

/// <summary>
/// Represents the page for controlling the TriloBot using a joystick.
/// Handles SignalR communication and joystick events.
/// </summary>
public partial class JoystickPage : ContentPage
{
    #region Private fields

    /// <summary>
    /// SignalR hub connection for real-time communication.
    /// </summary>
    private readonly HubConnection _hubConnection;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="JoystickPage"/> class and sets up the SignalR connection and joystick event handler.
    /// </summary>
    public JoystickPage()
    {
        InitializeComponent();

        // Initialize SignalR connection
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://pi5:6969/trilobotHub") // Replace <server-ip> with the actual server IP
            .Build();

        ConnectToHub();

        // Attach joystick event handler
        Joystick.OnJoystickChanged += Joystick_OnJoystickChanged;
    }

    #endregion

    #region SignalR Hub Connection

    /// <summary>
    /// Connects to the SignalR hub and starts distance updates.
    /// </summary>
    private async void ConnectToHub()
    {
        try
        {
            await _hubConnection.StartAsync();
            Console.WriteLine("Connected and subscript to SignalR Hub");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to SignalR Hub: {ex.Message}");
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles joystick movement events and sends updates to the SignalR hub only if the change exceeds a threshold.
    /// </summary>
    /// <param name="horizontal">The horizontal axis value.</param>
    /// <param name="vertical">The vertical axis value.</param>
    private async void Joystick_OnJoystickChanged(double horizontal, double vertical)
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

    #endregion
}