using System.Device.Gpio;

namespace TriloBot.Platform;

/// <summary>
/// Provides software-based PWM control for GPIO pins.
/// </summary>
public class SoftPwmChannel : IDisposable
{
    #region Private Fields

    /// <summary>
    /// The GPIO controller used for pin operations.
    /// </summary>
    private readonly GpioController _gpio;

    /// <summary>
    /// The GPIO pin number controlled by this PWM channel.
    /// </summary>
    private readonly int _pin;

    /// <summary>
    /// The PWM frequency in Hz.
    /// </summary>
    private readonly int _frequency;

    /// <summary>
    /// The current duty cycle (0-100).
    /// </summary>
    private double _dutyCycle;

    /// <summary>
    /// Cancellation token source for the PWM task.
    /// </summary>
    private readonly CancellationTokenSource _cancellationTokenSource;

    /// <summary>
    /// The background task running the PWM loop.
    /// </summary>
    private readonly Task _pwmTask;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new software PWM channel.
    /// </summary>
    /// <param name="gpio">The GPIO controller to use.</param>
    /// <param name="pin">The GPIO pin number to control.</param>
    /// <param name="frequency">The PWM frequency in Hz.</param>
    public SoftPwmChannel(GpioController gpio, int pin, int frequency)
    {
        _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
        _pin = pin;
        _frequency = frequency;
        _dutyCycle = Math.Clamp(0, 0, 100);

        // Make sure pin is set for output
        if (gpio.IsPinModeSupported(pin, PinMode.Output))
        {
            gpio.OpenPin(pin, PinMode.Output);
        }
        else
        {
            throw new InvalidOperationException($"Pin {pin} does not support output mode");
        }

        // Start PWM task
        _cancellationTokenSource = new CancellationTokenSource();
        _pwmTask = Task.Run(PwmLoop);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Changes the duty cycle of the PWM signal.
    /// </summary>
    /// <param name="dutyCycle">New duty cycle (0-100).</param>
    public void ChangeDutyCycle(double dutyCycle)
    {
        _dutyCycle = Math.Clamp(dutyCycle, 0, 100);
    }

    private async Task PwmLoop()
    {
        try
        {
            var periodUs = 1_000_000 / _frequency; // Period in microseconds
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_dutyCycle <= 0)
                {
                    _gpio.Write(_pin, PinValue.Low);
                    await Task.Delay(1);
                    continue;
                }

                if (_dutyCycle >= 100)
                {
                    _gpio.Write(_pin, PinValue.High);
                    await Task.Delay(1);
                    continue;
                }

                var onTimeUs = (int)(periodUs * _dutyCycle / 100.0);
                var offTimeUs = periodUs - onTimeUs;

                _gpio.Write(_pin, PinValue.High);
                await Task.Delay(TimeSpan.FromMicroseconds(onTimeUs));

                _gpio.Write(_pin, PinValue.Low);
                await Task.Delay(TimeSpan.FromMicroseconds(offTimeUs));
            }
        }
        catch (TaskCanceledException)
        {
            // Normal cancellation
        }
        finally
        {
            _gpio.Write(_pin, PinValue.Low);
        }
    }

    #endregion

    #region IDisposable Support

    /// <summary>
    /// Disposes the PWM channel, stops the PWM task, and releases all resources.
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        try
        {
            _pwmTask.Wait();
        }
        catch (AggregateException)
        {
            // Ignore task cancellation exceptions
        }
        _cancellationTokenSource.Dispose();
        _gpio.Write(_pin, PinValue.Low);

        GC.SuppressFinalize(this);
    }

    #endregion
}