namespace TriloBot.Maui.Controls;

/// <summary>
/// JoystickControl is a custom control for joystick interaction.
/// </summary>
public partial class JoystickControl : ContentView
{

    #region Private fields

    /// <summary>
    /// The radius of the joystick knob in pixels.
    /// </summary>
    private const double JoystickRadius = 35;

    /// <summary>
    /// SignalR hub connection service (singleton).
    /// </summary>
    private readonly Services.HubConnectionService _hubConnectionService;
    private readonly Microsoft.AspNetCore.SignalR.Client.HubConnection _hubConnection;

    #endregion

    #region Public properties

    /// <summary>
    /// Event that is triggered when the joystick position changes.
    /// </summary>
    public event Action<double, double>? OnJoystickChanged;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="JoystickControl"/> class.
    /// </summary>
    public JoystickControl()
    {
        InitializeComponent();

        // Get the singleton HubConnectionService from DI
        _hubConnectionService = Application.Current?.Handler?.MauiContext?.Services?.GetService(typeof(Services.HubConnectionService)) as Services.HubConnectionService
            ?? throw new InvalidOperationException("HubConnectionService not found in DI container.");
        _hubConnection = _hubConnectionService.HubConnection;
    }

    #endregion

    #region Event handlers

    /// <summary>
    /// Handles the pan gesture for the joystick and updates the knob position.
    /// </summary>
    /// <param name="sender">The object that raised the event.</param>
    /// <param name="e">The pan gesture event arguments.</param>
    private void OnJoystickPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Calculate the new position
                var newX = e.TotalX;
                var newY = e.TotalY;

                // Calculate distance from the center
                var distance = Math.Sqrt(newX * newX + newY * newY);

                // Constrain to circular boundary
                if (distance > JoystickRadius)
                {
                    var ratio = JoystickRadius / distance;
                    newX *= ratio;
                    newY *= ratio;
                }

                // Update knob position
                JoystickKnob.TranslationX = newX;
                JoystickKnob.TranslationY = newY;

                // Normalize values to -1 to 1 range
                var normalizedX = Math.Round(newX / JoystickRadius, 2);

                // Invert Y for the standard coordinate system
                var normalizedY = Math.Round(-newY / JoystickRadius, 2);

                // Trigger event
                OnJoystickChanged?.Invoke(normalizedX, normalizedY);
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                // Return knob to center
                JoystickKnob.TranslationX = 0;
                JoystickKnob.TranslationY = 0;

                // Trigger event with neutral position
                OnJoystickChanged?.Invoke(0, 0);
                break;
        }
    }

    #endregion
}