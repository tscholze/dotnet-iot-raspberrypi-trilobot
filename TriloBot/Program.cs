namespace TriloBot;

using Robot = TriloBot;

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Trilobot Police Lights Effect");
        Console.WriteLine("Press Ctrl+C to exit");

        using var robot = new Robot();
        Console.CancelKeyPress += (s, e) =>
        {
            robot.ClearUnderlighting();
        };

        try
        {
            while (true)
            {
                // Left side red, right side blue
                for (int i = 0; i < 6; i++)
                {
                    // Clear previous state
                    robot.ClearUnderlighting();

                    // Set red lights (rotating left to right)
                    int redPos = i;
                    robot.SetUnderlight(redPos, 255, 0, 0, false);
                    robot.SetUnderlight((redPos + 1) % 6, 128, 0, 0, false);

                    // Set blue lights (rotating right to left)
                    int bluePos = (12 - i) % 6;
                    robot.SetUnderlight(bluePos, 0, 0, 255, false);
                    robot.SetUnderlight((bluePos + 1) % 6, 0, 0, 128);  // Show on last update

                    Thread.Sleep(100);  // Adjust speed of rotation

                    Console.WriteLine($"Distance {robot.ReadDistance()} cm");
                }
            }
        }
        finally
        {
            robot.ClearUnderlighting();
        }

    }
}
