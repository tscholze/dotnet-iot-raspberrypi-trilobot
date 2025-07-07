namespace TriloBot;


public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Trilobot Police Lights Effect");
        Console.WriteLine("Press Ctrl+C to exit");


        var cancellationTokenSource = new CancellationTokenSource();

        using var robot = new TriloBot(cancellationTokenSource.Token);
        Console.CancelKeyPress += (s, e) =>
        {
            cancellationTokenSource.Cancel();
        };

        // robot.PlayPoliceEffect(cancellationTokenSource.Token);
        robot.StartDistanceMonitoring();

        robot.DistanceObservable.Subscribe(distance =>
        {
            Console.WriteLine($"Distance: {distance} cm");

            if (distance < 10)
            {
                Console.WriteLine("Obstacle detected! Stopping robot.");
                robot.Stop();
                robot.FillUnderlighting(255, 0, 0, true); // Set underlighting to red
            }
            else
            {
                robot.FillUnderlighting(0, 255, 0, true); // Set underlighting to red
            }
        });
    }
}
