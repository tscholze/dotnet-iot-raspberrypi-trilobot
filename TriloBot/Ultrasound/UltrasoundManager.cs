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
    public double ReadDistance()
    {
        const long offset = 190000; // Time in ns the measurement takes (prevents over estimates)
        double totalDistance = 0;
        int validSamples = 0;
        var watch = new Stopwatch();
        watch.Start();

        while (validSamples < UltrasoundConfiguration.Samples && watch.ElapsedMilliseconds < UltrasoundConfiguration.Timeout)
        {
            _gpio.Write(UltrasoundConfiguration.UltraTrigPin, PinValue.High);
            Thread.Sleep(TimeSpan.FromMilliseconds(0.01)); // 10 microseconds
            _gpio.Write(UltrasoundConfiguration.UltraTrigPin, PinValue.Low);

            var pulseStart = DateTime.UtcNow.Ticks;
            while (_gpio.Read(UltrasoundConfiguration.UltraEchoPin) == PinValue.Low)
            {
                if ((DateTime.UtcNow.Ticks - pulseStart) / TimeSpan.TicksPerMillisecond > UltrasoundConfiguration.Timeout)
                {
                    return 0;
                }
            }

            pulseStart = DateTime.UtcNow.Ticks;
            while (_gpio.Read(UltrasoundConfiguration.UltraEchoPin) == PinValue.High)
            {
                if ((DateTime.UtcNow.Ticks - pulseStart) / TimeSpan.TicksPerMillisecond > UltrasoundConfiguration.Timeout)
                {
                    return 0;
                }
            }

            var pulseEnd = DateTime.UtcNow.Ticks;
            var pulseDuration = ((pulseEnd - pulseStart) * 100) - offset; // Convert ticks to nanoseconds

            if (pulseDuration > 0 && pulseDuration < UltrasoundConfiguration.Timeout * 1000000)
            {
                totalDistance += pulseDuration * UltrasoundConfiguration.SpeedOfSoundCmNs / 2;
                validSamples++;
            }
        }

        return validSamples > 0 ? totalDistance / validSamples : 0;
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
