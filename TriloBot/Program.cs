namespace TriloBot;


public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Trilobot Police Lights Effect");
        Console.WriteLine("Press Ctrl+C to exit");


        var cancellationTokenSource = new CancellationTokenSource();

        using var robot = new TriloBot();
        Console.CancelKeyPress += (s, e) =>
        {
            cancellationTokenSource.Cancel();
        };

        robot.PlayPoliceEffect(cancellationTokenSource.Token);
    }
}
