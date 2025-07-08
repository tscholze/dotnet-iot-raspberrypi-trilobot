using TriloBot.Button;

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
            }
            else
            {
                // Set underlight to green
                robot.FillUnderlighting(0, 255, 0);
            }
        });

        robot.StartButtonMonitoring();

        var buttonListenerSubscription = robot.ButtonPressedObservable.Subscribe(button =>
        {
            Console.WriteLine($"Button pressed: {button}");

            switch (button)
            {
                case Buttons.ButtonA:
                    robot.FillUnderlighting(255, 255, 0); // Yellow
                    break;
                case Buttons.ButtonB:
                    robot.FillUnderlighting(255, 165, 0); // Orange

                    break;
                case Buttons.ButtonX:
                    robot.FillUnderlighting(0, 0, 5); // Blue

                    break;
                case Buttons.ButtonY:
                    robot.FillUnderlighting(255, 192, 203); // Pink

                    break;
                default:
                    robot.FillUnderlighting(255, 255, 255); // white

                    break;
            }
        });
        
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
