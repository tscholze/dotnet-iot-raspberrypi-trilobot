using System;
using System.Device.Gpio;
using System.Threading;

namespace TriloBot.Motor;

/// <summary>
/// Controls a servo motor using software PWM
/// </summary>
public class ServoMotor : IDisposable
{
    private readonly int _pin;
    private readonly GpioController _gpio;
    private readonly SoftPwmChannel _pwm;
    private readonly double _minAngle;
    private readonly double _maxAngle;
    private readonly double _minPulseWidth;
    private readonly double _maxPulseWidth;
    private bool _disposed;

    public ServoMotor(GpioController gpio, int pin, double minAngle = -90, double maxAngle = 90, double minPulseWidth = 0.0005, double maxPulseWidth = 0.0025)
    {
        _gpio = gpio ?? throw new ArgumentNullException(nameof(gpio));
        _pin = pin;
        _minAngle = minAngle;
        _maxAngle = maxAngle;
        _minPulseWidth = minPulseWidth;
        _maxPulseWidth = maxPulseWidth;
        _disposed = false;

        // Configure GPIO pin for output
        _gpio.OpenPin(_pin, PinMode.Output);

        // Create PWM channel with 50Hz frequency (standard for servos)
        _pwm = new SoftPwmChannel(_gpio, _pin, 50, 0);
    }

    public void SetAngle(double angle)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ServoMotor));

        // Clamp angle to valid range
        angle = Math.Clamp(angle, _minAngle, _maxAngle);

        // Convert angle to pulse width
        var normalizedAngle = (angle - _minAngle) / (_maxAngle - _minAngle);
        var pulseWidth = _minPulseWidth + normalizedAngle * (_maxPulseWidth - _minPulseWidth);

        // Convert pulse width to duty cycle (for 50Hz, period is 20ms)
        var dutyCycle = pulseWidth * 50.0 * 100.0; // Convert to percentage
        _pwm.ChangeDutyCycle(dutyCycle);
    }

    public void SetValue(double value)
    {
        // Convert -1 to 1 range to angle range
        var angle = value * (_maxAngle - _minAngle) / 2.0;
        SetAngle(angle);
    }

    public void Disable()
    {
        if (!_disposed)
        {
            _pwm.ChangeDutyCycle(0);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _pwm.Dispose();
            _disposed = true;
        }
    }
}