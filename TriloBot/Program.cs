using TriloBot.Button;

namespace TriloBot;

/// <summary>
/// Main entry point for the TriloBot Demo application.
/// For non-console based usage, consider using the SignalR / Blazor controls.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // Start the demo.
        // The actual intended usecase is to control the bot using 
        // the SignalR / Blazor controls.
        Console.WriteLine("Starting Demo of TriloBot...");

        var cancellationTokenSource = new CancellationTokenSource();
        using var robot = new TriloBot(cancellationTokenSource.Token);

        // Display system information
        Console.WriteLine("\n=== System Information ===");
        Console.WriteLine($"Hostname: {robot.GetHostname()}");
        Console.WriteLine($"Primary IP: {robot.GetPrimaryIpAddress()}");
        Console.WriteLine($"Uptime: {robot.GetSystemUptime()}");
        Console.WriteLine($"CPU Temperature: {robot.GetCpuTemperature():F1}°C");
        
        var loadAvg = robot.GetLoadAverages();
        Console.WriteLine($"Load Average: {loadAvg.load1min:F2} {loadAvg.load5min:F2} {loadAvg.load15min:F2}");

        var memInfo = robot.GetMemoryInfo();
        if (memInfo.TryGetValue("MemTotal", out var totalMem) && memInfo.TryGetValue("MemAvailable", out var availableMem))
        {
            var usedMem = totalMem - availableMem;
            var memPercent = (double)usedMem / totalMem * 100;
            Console.WriteLine($"Memory Usage: {usedMem:N0} KB / {totalMem:N0} KB ({memPercent:F1}%)");
        }

        Console.WriteLine("\nNetwork Interfaces:");
        foreach (var (interfaceName, addresses) in robot.GetNetworkInterfaces())
        {
            Console.WriteLine($"  {interfaceName}: {string.Join(", ", addresses)}");
        }

        // Start system monitoring for real-time updates
        Console.WriteLine("\n=== Starting System Monitoring ===");
        robot.StartSystemMonitoring(intervalMs: 3000);

        // Subscribe to system observables
        var cpuSubscription = robot.CpuUsageObservable.Subscribe(cpu =>
            Console.WriteLine($"CPU Usage: {cpu:F1}%"));

        var memorySubscription = robot.MemoryUsageObservable.Subscribe(memory =>
            Console.WriteLine($"Memory Usage: {memory:F1}%"));

        var tempSubscription = robot.CpuTemperatureObservable.Subscribe(temp =>
            Console.WriteLine($"CPU Temperature: {temp:F1}°C"));

        Console.WriteLine("System monitoring started. Press any key to continue with robot demo...\n");
        Console.ReadKey();

        // Clean up system monitoring subscriptions
        cpuSubscription.Dispose();
        memorySubscription.Dispose();
        tempSubscription.Dispose();

        // Start distance monitoring
        robot.StartDistanceMonitoring();

        // Subscribe to objectTooNearObserver
        var tooNearSubscription = robot.ObjectTooNearObservable.Subscribe(tooNear =>
        {
            // Change underlighting based on distance
            // If the object is too near, set underlighting to red, otherwise set it to green
            if (tooNear)
            {
                robot.FillUnderlighting(255, 0, 0);
            }
            else
            {
                robot.FillUnderlighting(0, 255, 0);
            }
        });

        // Start button monitoring
        robot.StartButtonMonitoring();
        var buttonListenerSubscription = robot.ButtonPressedObservable.Subscribe(button =>
        {
            // Change underlighting based on button pressed
            // Button A: Yellow, Button B: Orange, Button X: Blue, Button Y: Pink
            // Default: White
            switch (button)
            {
                case Buttons.ButtonA:
                    robot.FillUnderlighting(255, 255, 0);
                    break;
                case Buttons.ButtonB:
                    robot.FillUnderlighting(255, 165, 0);
                    break;
                case Buttons.ButtonX:
                    robot.FillUnderlighting(0, 0, 5);
                    break;
                case Buttons.ButtonY:
                    robot.FillUnderlighting(255, 192, 203); // Pink
                    break;
                default:
                    robot.FillUnderlighting(255, 255, 255);
                    break;
            }
        });

        // Handle cancellation gracefully
        Console.CancelKeyPress += (s, e) =>
        {
            cancellationTokenSource.Cancel();
            tooNearSubscription.Dispose();
            buttonListenerSubscription.Dispose();
            robot.Dispose();
        };

        // Keep the program running until cancellation is requested
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            Thread.Sleep(100);
        }
    }
}
