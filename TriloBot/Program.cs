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

        // Subscribe to objectTooNearObserver
        var tooNearSubscription = robot.ObjectTooNearObservable.Subscribe(tooNear =>
        {
            Console.WriteLine($"Object too near: {tooNear}");

            if (tooNear)
            {
                // Set underlight to red
                robot.FillUnderlighting(255, 0, 0);
                robot.CurveForwardRight();
            }
            else
            {
                // Set underlight to green
                robot.FillUnderlighting(0, 255, 0);
                robot.Forward();
            }
        });

        robot.Forward(); // Start moving forward

        Console.CancelKeyPress += (s, e) =>
        {
            cancellationTokenSource.Cancel();
            tooNearSubscription.Dispose();
        };

        // Keep the program running until cancellation is requested
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            Thread.Sleep(100);
        }
    }
}
