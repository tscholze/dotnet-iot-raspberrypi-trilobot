using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace TriloBot.Maui
{
    public partial class MainPage : ContentPage
    {
        #region Fields
        private readonly HubConnection _hubConnection;
        private IAsyncEnumerable<double>? _distanceStream;

        #endregion
        
        #region Constructors
        
        public MainPage()
        {
            InitializeComponent();

            // Initialize SignalR connection
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("http://pi5:6969/trilobotHub") // Replace <server-ip> with the actual server IP
                .Build();

            ConnectToHub();

            // Attach SizeChanged event handler
            this.SizeChanged += OnSizeChanged;
        }
        
        #endregion
        
        #region Event Handlers

        private void OnSizeChanged(object? sender, EventArgs e)
        {
            var borderWidth = this.Width - 40;
            CameraBorder.WidthRequest = borderWidth;
            CameraBorder.HeightRequest = borderWidth * 9 / 16;

            var webViewWidth = borderWidth - 4;
            CameraWebView.WidthRequest = webViewWidth;
            CameraWebView.HeightRequest = webViewWidth * 9 / 16;

            Console.WriteLine($"CameraWebView size: {CameraWebView.WidthRequest}x{CameraWebView.HeightRequest}");
            Console.WriteLine($"MainPage size: {this.Width}x{this.Height}");
        }
        
        #endregion
        
        #region SignalR Hub Connection

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

        private async void OnMoveForwardClicked(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("Forward");
        }

        private async void OnMoveBackwardClicked(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("Backward");
        }

        private async void OnTurnLeftClicked(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("TurnLeft");
        }

        private async void OnTurnRightClicked(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("TurnRight");
        }

        private async void OnStopClicked(object sender, EventArgs e)
        {
            await _hubConnection.InvokeAsync("Stop");
        }
        
        #endregion
    }
}
