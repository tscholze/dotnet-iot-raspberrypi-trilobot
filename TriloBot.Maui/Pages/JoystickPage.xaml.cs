using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace TriloBot.Maui.Pages;

public partial class JoystickPage : ContentPage
{
    #region Private fields

    /// <summary>
    /// SignalR hub connection for real-time communication.
    /// </summary>
    private readonly HubConnection _hubConnection;

    #endregion

    #region Constructor

    public JoystickPage()
    {
        InitializeComponent();

        // Initialize SignalR connection
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("http://pi5:6969/trilobotHub") // Replace <server-ip> with the actual server IP
            .Build();

        ConnectToHub();

        Joystick.OnJoystickChanged += Joystick_OnJoystickChanged;
    }

    private void Joystick_OnJoystickChanged(string horizontal, double vertical, double rotation)
    {
        Console.WriteLine($"Joystick moved: Horizontal={horizontal}, Vertical={vertical}, Rotation={rotation}");
        _ = _hubConnection.InvokeAsync("Move", horizontal, vertical);
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
}