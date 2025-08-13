using Microsoft.AspNetCore.SignalR.Client;
using TriloBot.Maui.Services;

namespace TriloBot.Maui.Pages
{
    /// <summary>
    /// Represents the main page of the TriloBot application.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        #region Private fields

        /// <summary>
        /// Hub connection service for managing SignalR connections.
        /// </summary>  
        private readonly HubConnectionService _hubConnectionService;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage(HubConnectionService hubConnectionService)
        {
            // Start view lifecycle
            InitializeComponent();

            // Ensure dependency is available and setup
            _hubConnectionService = hubConnectionService ?? throw new ArgumentNullException(nameof(hubConnectionService), "HubConnectionService cannot be null.");
            _hubConnectionService.IsConnectedObservable.Subscribe(OnIsHubConnectedChanged);

            // Attach SizeChanged event handler
            SizeChanged += OnSizeChanged;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the size change event to adjust the layout of the camera view.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnSizeChanged(object? sender, EventArgs e)
        {
            // Adjust the camera view size based on the current width of the page.
            // -40 = padding for borders
            // -4 = border width for webview
            if (Width <= 0) return; // Prevent division by zero
            var borderWidth = Width - 40;
            CameraBorder.WidthRequest = borderWidth;
            CameraBorder.HeightRequest = borderWidth * 9 / 16;

            var webViewWidth = borderWidth - 4;
            CameraWebView.WidthRequest = webViewWidth;
            CameraWebView.HeightRequest = webViewWidth * 9 / 16;
        }

        #endregion

        #region SignalR Hub Connection

        /// <summary>
        /// Handles changes in the SignalR hub connection status.
        /// </summary>
        private void OnIsHubConnectedChanged(bool isConnected)
        {
            if (isConnected)
            {
                ConnectionStatusLabel.Text = "Yes";
                ConnectionStatusLabel.TextColor = Colors.Green;
            }
            else
            {
                ConnectionStatusLabel.Text = "No";
                ConnectionStatusLabel.TextColor = Colors.Red;
            }
        }

        #endregion

        #region Button Handlers

        /// <summary>
        /// Handles the "Move Forward" button click event.
        /// </summary>
        private async void OnMoveForwardClicked(object sender, EventArgs e)
        {
            await SafeInvokeAsync("Forward");
        }

        /// <summary>
        /// Handles the "Move Backward" button click event.
        /// </summary>
        private async void OnMoveBackwardClicked(object sender, EventArgs e)
        {
            await SafeInvokeAsync("Backward");
        }

        /// <summary>
        /// Handles the "Turn Left" button click event.
        /// </summary>
        private async void OnTurnLeftClicked(object sender, EventArgs e)
        {
            await SafeInvokeAsync("TurnLeft");
        }

        /// <summary>
        /// Handles the "Turn Right" button click event.
        /// </summary>
        private async void OnTurnRightClicked(object sender, EventArgs e)
        {
            await SafeInvokeAsync("TurnRight");
        }

        /// <summary>
        /// Handles the "Stop" button click event.
        /// </summary>
        private async void OnStopClicked(object sender, EventArgs e)
        {
            await SafeInvokeAsync("Stop");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper method to safely invoke a SignalR hub method with error handling.
        /// </summary>
        /// <param name="methodName">The name of the hub method to invoke.</param>
        /// <param name="args">Optional arguments to pass to the hub method.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task SafeInvokeAsync(string methodName, params object[] args)
        {
            try
            {
                await _hubConnection.InvokeAsync(methodName, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error invoking '{methodName}': {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Event handler to turn all lights on (white).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void OnLightAllOn(object? sender, EventArgs e)
        {
            SafeInvokeAsync("FillUnderlighting", 255, 255, 255).ConfigureAwait(false);
        }

        /// <summary>
        /// Event handler to turn all lights off.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void OnLightAllOff(object? sender, EventArgs e)
        {
            SafeInvokeAsync("FillUnderlighting", 0, 0, 0).ConfigureAwait(false);
        }

        /// <summary>
        /// Event handler to turn all lights purple.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments.</param>
        private void OnLightAllPurple(object? sender, EventArgs e)
        {
            SafeInvokeAsync("FillUnderlighting", 128, 0, 128).ConfigureAwait(false);
        }
    }
}
