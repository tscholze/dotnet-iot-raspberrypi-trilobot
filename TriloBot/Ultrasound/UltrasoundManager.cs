using System.Device.Gpio;
using System.Diagnostics;

namespace TriloBot.Ultrasound;

/// <summary>
/// Manages ultrasonic distance measurement using a trigger and echo pin.
/// </summary>
public sealed class UltrasoundManager : IDisposable
{
    #region Private Fields

    /// <summary>
    /// The GPIO controller used for ultrasound pin operations.
    /// </summary>
    private readonly GpioController _gpio;

    #endregion

    #region Constants

    /// <summary>
    /// GPIO pin number for the trigger signal of the ultrasound sensor.
    /// </summary>
    private const int TriggerPin = 13;

    /// <summary>
    /// GPIO pin number for the echo signal of the ultrasound sensor.
    /// </summary>
    private const int EchoPin = 25;

    /// <summary>
    /// Speed of sound in centimeters per nanosecond.
    /// </summary>
    private const double SpeedOfSoundCmPerNs = 343 * 100.0 / 1E9; // 0.0000343 cm/ns

    /// <summary>
    /// Timeout in milliseconds for the ultrasound sensor to receive an echo.
    /// </summary>
    private const int EchoTimeoutMs = 50;

    /// <summary>
    /// Number of samples to take for averaging distance measurements.
    /// </summary>
    private const int SampleCount = 3;

    /// <summary>
    /// Default offset in nanoseconds to subtract from the pulse duration to account for sensor timing inaccuracies.
    /// </summary>
    private const long PulseOffsetNs = 190_000;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="UltrasoundManager"/> class and opens the trigger and echo pins.
    /// </summary>
    /// <param name="gpio">The GPIO controller to use for pin operations.</param>
    /// <summary>
    /// Initializes a new instance of the <see cref="UltrasoundManager"/> class and opens the trigger and echo pins.
    /// </summary>
    /// <param name="gpio">The GPIO controller to use for pin operations.</param>
    public UltrasoundManager(GpioController gpio)
    {
        _gpio = gpio;
        _gpio.OpenPin(TriggerPin, PinMode.Output);
        _gpio.OpenPin(EchoPin, PinMode.Input);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Reads the distance using the ultrasonic sensor, averaging multiple samples, with timeout and offset logic based on Trilobot Python.
    /// </summary>
    /// <returns>Average distance in centimeters, or 0 if no valid readings.</returns>
    /// <summary>
    /// Reads the distance using the ultrasonic sensor, averaging multiple samples, with timeout and offset logic.
    /// </summary>
    /// <returns>Average distance in centimeters, or 0 if no valid readings.</returns>
    public double ReadDistance()
    {
        var startTime = Stopwatch.GetTimestamp();
        var timeElapsedNs = 0L;
        var validSampleCount = 0;
        var totalPulseDurations = 0L;
        var timeoutTotalNs = EchoTimeoutMs * 1_000_000L; // ms to ns
        var ticksPerNs = Stopwatch.Frequency / 1_000_000_000.0;

        // Busy-wait for a specified number of ticks
        static void BusyWait(long durationTicks)
        {
            var t0 = Stopwatch.GetTimestamp();
            while ((Stopwatch.GetTimestamp() - t0) < durationTicks) { }
        }

        // Loop until the required number of samples is collected or the total timeout is reached
        while (validSampleCount < SampleCount && timeElapsedNs < timeoutTotalNs)
        {
            // 1. Ensure the trigger pin is low for a short period (~2us)
            _gpio.Write(TriggerPin, PinValue.Low);
            BusyWait(Stopwatch.Frequency / 500_000);

            // 2. Send a 10 microsecond pulse on the trigger pin
            _gpio.Write(TriggerPin, PinValue.High);
            BusyWait(Stopwatch.Frequency / 100_000);
            _gpio.Write(TriggerPin, PinValue.Low);

            // 3. Wait for the echo pin to go high (start of echo)
            var timeoutWatch = Stopwatch.StartNew();
            while (_gpio.Read(EchoPin) == PinValue.Low && timeoutWatch.ElapsedMilliseconds <= EchoTimeoutMs) { }
            if (_gpio.Read(EchoPin) == PinValue.Low)
            {
                // Timed out waiting for echo to go high
                timeElapsedNs = (long)((Stopwatch.GetTimestamp() - startTime) / ticksPerNs);
                continue;
            }

            var pulseStart = Stopwatch.GetTimestamp();

            // 4. Wait for the echo pin to go low (end of echo)
            timeoutWatch.Restart();
            while (_gpio.Read(EchoPin) == PinValue.High && timeoutWatch.ElapsedMilliseconds <= EchoTimeoutMs) { }
            if (_gpio.Read(EchoPin) == PinValue.High)
            {
                // Timed out waiting for echo to go low
                timeElapsedNs = (long)((Stopwatch.GetTimestamp() - startTime) / ticksPerNs);
                continue;
            }

            // 5. Calculate the pulse duration in nanoseconds
            var pulseEnd = Stopwatch.GetTimestamp();
            var pulseDurationNs = (long)((pulseEnd - pulseStart) / ticksPerNs) - PulseOffsetNs;
            pulseDurationNs = Math.Max(pulseDurationNs, 0);

            // Only count reading if achieved in less than timeout total time
            if (pulseDurationNs < timeoutTotalNs)
            {
                totalPulseDurations += pulseDurationNs;
                validSampleCount++;
            }

            timeElapsedNs = (long)((Stopwatch.GetTimestamp() - startTime) / ticksPerNs);
        }

        // Calculate the average distance based on the total pulse durations
        if (validSampleCount == 0)
            return 0.0;

        // Round to a 10th of a centimeter to avoid jitter in readings
        var rawDistance = totalPulseDurations * SpeedOfSoundCmPerNs / (2 * validSampleCount);
        return Math.Round(rawDistance, 1);
    }

    #endregion

    #region IDisposable Support

    /// <summary>
    /// Disposes the UltrasoundManager and closes the trigger and echo pins.
    /// </summary>
    /// <summary>
    /// Disposes the UltrasoundManager and closes the trigger and echo pins.
    /// </summary>
    public void Dispose()
    {
        _gpio.ClosePin(TriggerPin);
        _gpio.ClosePin(EchoPin);
        GC.SuppressFinalize(this);
    }

    #endregion
}
