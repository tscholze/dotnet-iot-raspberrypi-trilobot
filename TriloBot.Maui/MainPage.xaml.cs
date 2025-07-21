using Microsoft.AspNetCore.SignalR.Client;

namespace TriloBot.Maui
{
    /// <summary>
    /// Represents the main page of the TriloBot application.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        #region Fields

        /// <summary>
        /// SignalR hub connection for real-time communication.
        /// </summary>
        private readonly HubConnection _hubConnection;

        /// <summary>
        /// Stream of distance updates from the SignalR hub.
        /// </summary>
        private IAsyncEnumerable<double>? _distanceStream;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            // Initialize SignalR connection
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://pi5:6969/trilobotHub") // Replace <server-ip> with the actual server IP
                .Build();

            ConnectToHub();

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
        /// Connects to the SignalR hub and starts distance updates.
        /// </summary>
        private async void ConnectToHub()
        {
            try
            {
                await _hubConnection.StartAsync();
                Console.WriteLine("Connected to SignalR Hub");

                await StartDistanceUpdates();
                Console.WriteLine("Distance updates started");
            }
            catch (Exception ex)
            {
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
                _distanceStream = _hubConnection.StreamAsync<double>("DistanceStream");

                _ = Task.Run(async () =>
                {
                    await foreach (var distance in _distanceStream)
                    {
                        MainThread.BeginInvokeOnMainThread(() => { DistanceCardLabel.Text = $"Distance: {distance:F2} cm"; });
                    }
                });

                await _hubConnection.InvokeAsync("StartDistanceMonitoring");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error subscribing to distance updates: {ex.Message}");
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
    }
}
