using System.Device.Gpio;
using System.Diagnostics;

namespace TriloBot.Ultrasound;

/// <summary>
/// Manages ultrasonic distance measurement using a trigger and echo pin.
/// </summary>
public class UltrasoundManager : IDisposable
{
    #region Private Fields

    /// <summary>
    /// The GPIO controller used for ultrasound pin operations.
    /// </summary>
    private readonly GpioController _gpio;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="UltrasoundManager"/> class and opens the trigger and echo pins.
    /// </summary>
    /// <param name="gpio">The GPIO controller to use for pin operations.</param>
    public UltrasoundManager(GpioController gpio)
    {
        _gpio = gpio;
        _gpio.OpenPin(UltrasoundConfiguration.UltraTrigPin, PinMode.Output);
        _gpio.OpenPin(UltrasoundConfiguration.UltraEchoPin, PinMode.Input);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Reads the distance using the ultrasonic sensor.
    /// </summary>
    /// <returns>Distance in centimeters, or 0 if no valid reading.</returns>
    /// <remarks>
    /// Steps performed by this function:
    /// 1. Ensure the trigger pin is low for a short period.
    /// 2. Send a 10 microsecond pulse on the trigger pin.
    /// 3. Wait for the echo pin to go high (start of echo).
    /// 4. Measure how long the echo pin stays high (duration of echo).
    /// 5. Calculate the distance based on the duration.
    /// 6. Return the measured distance in centimeters, or 0 if invalid.
    /// </remarks>
    public double ReadDistance()
    {
        // 1. Ensure the trigger pin is low for a short period
        _gpio.Write(UltrasoundConfiguration.UltraTrigPin, PinValue.Low);
        var t0 = Stopwatch.GetTimestamp();
        while ((Stopwatch.GetTimestamp() - t0) < (Stopwatch.Frequency / 500000)) { } // ~2us

        // 2. Send a 10 microsecond pulse on the trigger pin
        _gpio.Write(UltrasoundConfiguration.UltraTrigPin, PinValue.High);
        var pulseWatch = Stopwatch.StartNew();
        while (pulseWatch.ElapsedTicks < (Stopwatch.Frequency / 100000)) { } // ~10us busy-wait
        _gpio.Write(UltrasoundConfiguration.UltraTrigPin, PinValue.Low);

        // 3. Wait for the echo pin to go high (start of echo)
        var timeoutWatch = Stopwatch.StartNew();
        while (_gpio.Read(UltrasoundConfiguration.UltraEchoPin) == PinValue.Low)
        {
            if (timeoutWatch.ElapsedMilliseconds > UltrasoundConfiguration.Timeout)
            {
                System.Console.WriteLine("Ultrasound: Echo pin never went high (timeout)");
                return 0;
            }
        }

        // 4. Measure how long the echo pin stays high (duration of echo)
        var echoStart = Stopwatch.GetTimestamp();
        timeoutWatch.Restart();
        while (_gpio.Read(UltrasoundConfiguration.UltraEchoPin) == PinValue.High)
        {
            if (timeoutWatch.ElapsedMilliseconds > UltrasoundConfiguration.Timeout)
            {
                System.Console.WriteLine("Ultrasound: Echo pin stuck high (timeout)");
                return 0;
            }
        }
        var echoEnd = Stopwatch.GetTimestamp();

        // 5. Calculate the distance based on the duration
        double pulseDuration = (echoEnd - echoStart) / (double)Stopwatch.Frequency;
        double distance = (pulseDuration * 34300) / 2.0;

        // 6. Return the measured distance in centimeters, or 0 if invalid
        if (distance > 2 && distance < 400) // Typical HC-SR04 range
        {
            return distance;
        }
        else
        {
            System.Console.WriteLine($"Ultrasound: Out of range or invalid reading: {distance:F2} cm");
            return 0;
        }
    }

    #endregion

    #region IDisposable Support

    /// <summary>
    /// Disposes the UltrasoundManager and closes the trigger and echo pins.
    /// </summary>
    public void Dispose()
    {
        _gpio.ClosePin(UltrasoundConfiguration.UltraTrigPin);
        _gpio.ClosePin(UltrasoundConfiguration.UltraEchoPin);

        GC.SuppressFinalize(this);
    }

    #endregion
}
