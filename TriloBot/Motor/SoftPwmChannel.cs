using System;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;

namespace TriloBot.Motor;

/// <summary>
/// Provides software-based PWM control for GPIO pins
/// </summary>
internal class SoftPwmChannel : IDisposable
{
    private readonly GpioController _gpio;
    private readonly int _pin;
    private readonly int _frequency;
    private double _dutyCycle;
    private bool _disposed;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _pwmTask;

    /// <summary>
    /// Creates a new software PWM channel
    /// </summary>
    /// <param name="gpio">The GPIO controller to use</param>
    /// <param name="pin">The GPIO pin number to control</param>
    /// <param name="frequency">The PWM frequency in Hz</param>
    /// <param name="initialDutyCycle">Initial duty cycle (0-100)</param>
    public SoftPwmChannel(GpioController gpio, int pin, int frequency, double initialDutyCycle = 0)
    {
        _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
        _pin = pin;
        _frequency = frequency;
        _dutyCycle = Math.Clamp(initialDutyCycle, 0, 100);
        _disposed = false;

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

    /// <summary>
    /// Changes the duty cycle of the PWM signal
    /// </summary>
    /// <param name="dutyCycle">New duty cycle (0-100)</param>
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

    public void Dispose()
    {
        if (!_disposed)
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
            _disposed = true;
        }
    }
}