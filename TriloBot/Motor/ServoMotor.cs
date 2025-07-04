using System.Device.Gpio;
using TriloBot.Platform;

namespace TriloBot.Motor;

/// <summary>
/// Controls a servo motor using software PWM.
/// </summary>
public class ServoMotor : IDisposable
{
    #region Private Fields

    /// <summary>
    /// The GPIO pin used for the servo signal.
    /// </summary>
    private readonly int _pin;

    /// <summary>
    /// The GPIO controller used for pin operations.
    /// </summary>
    private readonly GpioController _gpio;

    /// <summary>
    /// The PWM channel used to control the servo.
    /// </summary>
    private readonly SoftPwmChannel _pwm;

    /// <summary>
    /// Minimum angle the servo can rotate to (degrees).
    /// </summary>
    private readonly double _minAngle = -90;

    /// <summary>
    /// Maximum angle the servo can rotate to (degrees).
    /// </summary>
    private readonly double _maxAngle = 90;

    /// <summary>
    /// Minimum pulse width for the servo (seconds).
    /// </summary>
    private readonly double _minPulseWidth = 0.0005;

    /// <summary>
    /// Maximum pulse width for the servo (seconds).
    /// </summary>
    private readonly double _maxPulseWidth = 0.002;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ServoMotor"/> class and sets up the PWM channel.
    /// </summary>
    /// <param name="gpio">The GPIO controller to use for pin operations.</param>
    /// <param name="pin">The GPIO pin used for the servo signal.</param>
    public ServoMotor(GpioController gpio, int pin)
    {
        _gpio = gpio;
        _pin = pin;

        // Configure GPIO pin for output
        _gpio.OpenPin(_pin, PinMode.Output);

        // Create PWM channel with 50Hz frequency (standard for servos)
        _pwm = new SoftPwmChannel(_gpio, _pin, 50);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets the servo to the specified angle.
    /// </summary>
    /// <param name="angle">The angle to set (degrees).</param>
    public void SetAngle(double angle)
    {
        // Clamp angle to valid range
        angle = Math.Clamp(angle, _minAngle, _maxAngle);

        // Convert angle to pulse width
        var normalizedAngle = (angle - _minAngle) / (_maxAngle - _minAngle);
        var pulseWidth = _minPulseWidth + normalizedAngle * (_maxPulseWidth - _minPulseWidth);

        // Convert pulse width to duty cycle (for 50Hz, period is 20ms)
        var dutyCycle = pulseWidth * 50.0 * 100.0; // Convert to percentage
        _pwm.ChangeDutyCycle(dutyCycle);
    }

    /// <summary>
    /// Sets the servo position using a normalized value (-1 to 1).
    /// </summary>
    /// <param name="value">Normalized value (-1 for min, 1 for max, 0 for center).</param>
    public void SetValue(double value)
    {
        // Convert -1 to 1 range to angle range
        var angle = value * (_maxAngle - _minAngle) / 2.0;
        SetAngle(angle);
    }
    
    /// <summary>
    /// Disables the servo by setting the duty cycle to 0.
    /// </summary>
    public void Disable()
    {
        _pwm.ChangeDutyCycle(0);
    }

    #endregion

    #region IDisposable Support

    /// <summary>
    /// Disposes the ServoMotor, disables the servo, and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Disable();
        _pwm.Dispose();
        _gpio.ClosePin(_pin);
        GC.SuppressFinalize(this);
    }

    #endregion
}