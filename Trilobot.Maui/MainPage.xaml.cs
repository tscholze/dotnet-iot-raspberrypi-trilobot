using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Trilobot.Maui
{
    public partial class MainPage : ContentPage
    {
        private readonly HubConnection _hubConnection;

        public MainPage()
		{
			InitializeComponent();

			// Initialize SignalR connection
			_hubConnection = new HubConnectionBuilder()
				.WithUrl("http://pi5:6969/trilobotHub") // Replace <server-ip> with the actual server IP
				.Build();

			ConnectToHub();
		}

        private async void ConnectToHub()
        {
            try
            {
                await _hubConnection.StartAsync();
                Console.WriteLine("Connected to SignalR Hub");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to SignalR Hub: {ex.Message}");
            }
        }

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
    }
}
