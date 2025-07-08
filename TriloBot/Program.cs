namespace TriloBot;


public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Trilobot Police Lights Effect");
        Console.WriteLine("Press Ctrl+C to exit");


        var cancellationTokenSource = new CancellationTokenSource();

        using var robot = new TriloBot(cancellationTokenSource.Token);

        // Start distance monitoring
        robot.StartDistanceMonitoring();

        // Subscribe to distanceObserver
        var distanceSubscription = robot.distanceObserver.Subscribe(distance =>
        {
            if (distance < 10)
            {
                // Set underlight to red
                robot.FillUnderlighting(255, 0, 0);
            }
            else
            {
                // Set underlight to green
                robot.FillUnderlighting(0, 255, 0);
            }
        });
        
        Console.CancelKeyPress += (s, e) =>
        {
            cancellationTokenSource.Cancel();
            distanceSubscription.Dispose();
        };

        // Keep the program running until cancellation is requested
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            Thread.Sleep(100);
        }
    }
}
