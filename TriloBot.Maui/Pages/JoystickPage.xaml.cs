using TriloBot.Maui.Services;

namespace TriloBot.Maui.Pages;

/// <summary>
/// Represents the page for controlling the TriloBot using a joystick.
/// Handles SignalR communication and joystick events.
/// </summary>
public partial class JoystickPage : ContentPage
{
    #region Private fields

    /// <summary>
    /// Hub connection service for managing SignalR connections.
    /// </summary>
    private readonly HubConnectionService _hubConnectionService;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="JoystickPage"/> class and sets up the SignalR connection and joystick event handler.
    /// </summary>
    public JoystickPage(HubConnectionService hubConnectionService)
    {
        // Start view lifecycle
        InitializeComponent();

        // Ensure dependency is available
        _hubConnectionService = hubConnectionService ?? throw new ArgumentNullException(nameof(hubConnectionService), "HubConnectionService cannot be null.");

        // Attach joystick event handler
        Joystick.OnJoystickChanged += Joystick_OnJoystickChanged;
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
            await _hubConnectionService.InvokeMove(horizontal, vertical);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error sending joystick movement: {e.Message}");
        }
    }

    #endregion
}