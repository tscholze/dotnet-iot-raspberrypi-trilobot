using System;
using System.Device.Gpio;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace TriloBot;

public class TriloBot : IDisposable
{
    // Constants for buttons
    public const int ButtonA = 0;
    public const int ButtonB = 1;
    public const int ButtonX = 2;
    public const int ButtonY = 3;
    public const int NumButtons = 4;

    // Underlighting LED locations
    public const int LightFrontRight = 0;
    public const int LightFrontLeft = 1;
    public const int LightMiddleLeft = 2;
    public const int LightRearLeft = 3;
    public const int LightRearRight = 4;
    public const int LightMiddleRight = 5;
    public const int NumUnderlights = 6;

    // Useful underlighting groups
    public static readonly int[] LightsLeft = { LightFrontLeft, LightMiddleLeft, LightRearLeft };
    public static readonly int[] LightsRight = { LightFrontRight, LightMiddleRight, LightRearRight };
    public static readonly int[] LightsFront = { LightFrontLeft, LightFrontRight };
    public static readonly int[] LightsMiddle = { LightMiddleLeft, LightMiddleRight };
    public static readonly int[] LightsRear = { LightRearLeft, LightRearRight };
    public static readonly int[] LightsLeftDiagonal = { LightFrontLeft, LightRearRight };
    public static readonly int[] LightsRightDiagonal = { LightFrontRight, LightRearLeft };

    // Motor names
    public const int MotorLeft = 0;
    public const int MotorRight = 1;
    public const int NumMotors = 2;

    // Pin definitions
    private const int ButtonAPin = 5;
    private const int ButtonBPin = 6;
    private const int ButtonXPin = 16;
    private const int ButtonYPin = 24;

    private const int LedAPin = 23;
    private const int LedBPin = 22;
    private const int LedXPin = 17;
    private const int LedYPin = 27;

    private const int MotorEnPin = 26;
    private const int MotorLeftP = 8;
    private const int MotorLeftN = 11;
    private const int MotorRightP = 10;
    private const int MotorRightN = 9;

    private const int UltraTrigPin = 13;
    private const int UltraEchoPin = 25;

    private const int ServoPin = 12;

    private const int UnderlightingEnPin = 7;

    // Speed of sound in cm/ns
    private const double SpeedOfSoundCmNs = 343 * 100.0 / 1E9; // 0.0000343 cm/ns

    private readonly GpioController _gpio;
    private readonly int[] _buttons;
    private readonly int[] _leds;
    private readonly Dictionary<int, SoftPwmChannel> _ledPwmMapping;
    private readonly Dictionary<int, SoftPwmChannel> _motorPwmMapping;
    private SN3218 _sn3218 = null!;
    private byte[] _underlight = null!;
    private ServoMotor? _servo;
    private bool _disposed;

    public TriloBot()
    {
        _gpio = new GpioController();
        _disposed = false;

        // Setup button pins
        _buttons = new[] { ButtonAPin, ButtonBPin, ButtonXPin, ButtonYPin };
        foreach (var pin in _buttons)
        {
            _gpio.OpenPin(pin, PinMode.InputPullUp);
        }

        // Setup LED pins
        _leds = new[] { LedAPin, LedBPin, LedXPin, LedYPin };
        _ledPwmMapping = new Dictionary<int, SoftPwmChannel>();
        foreach (var pin in _leds)
        {
            _gpio.OpenPin(pin, PinMode.Output);
            _ledPwmMapping[pin] = new SoftPwmChannel(_gpio, pin, 2000, 0);
        }

        // Setup motor pins
        _gpio.OpenPin(MotorEnPin, PinMode.Output);
        _gpio.OpenPin(MotorLeftP, PinMode.Output);
        _gpio.OpenPin(MotorLeftN, PinMode.Output);
        _gpio.OpenPin(MotorRightP, PinMode.Output);
        _gpio.OpenPin(MotorRightN, PinMode.Output);

        _motorPwmMapping = new Dictionary<int, SoftPwmChannel>
        {
            [MotorLeftP] = new SoftPwmChannel(_gpio, MotorLeftP, 100, 0),
            [MotorLeftN] = new SoftPwmChannel(_gpio, MotorLeftN, 100, 0),
            [MotorRightP] = new SoftPwmChannel(_gpio, MotorRightP, 100, 0),
            [MotorRightN] = new SoftPwmChannel(_gpio, MotorRightN, 100, 0)
        };

        // Initialize SN3218 LED driver
        try
        {
            _sn3218 = new SN3218();
            _underlight = new byte[18];
            _sn3218.Output(_underlight);
            _sn3218.EnableLeds(0b111111111111111111);
            ShowUnderlighting(); // Enable the lights initially
            Console.WriteLine("Successfully initialized SN3218 LED driver");
        }
        catch (System.IO.IOException ex)
        {
            Console.WriteLine($"Error initializing SN3218 LED driver: {ex.Message}");
            Console.WriteLine("Please check I2C connections and address");
            throw;
        }

        // Setup ultrasonic sensor pins
        _gpio.OpenPin(UltraTrigPin, PinMode.Output);
        _gpio.OpenPin(UltraEchoPin, PinMode.Input);
    }

    public bool ReadButton(int button)
    {
        if (button < 0 || button >= NumButtons)
            throw new ArgumentOutOfRangeException(nameof(button), "Button must be 0-3");

        return _gpio.Read(_buttons[button]) == PinValue.Low;
    }

    public void SetButtonLed(int buttonLed, double value)
    {
        if (buttonLed < 0 || buttonLed >= NumButtons)
            throw new ArgumentOutOfRangeException(nameof(buttonLed), "Button LED must be 0-3");

        if (value < 0.0 || value > 1.0)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0.0 and 1.0");

        _ledPwmMapping[_leds[buttonLed]].ChangeDutyCycle(value * 100.0);
    }

    public void SetMotorSpeed(int motor, double speed)
    {
        if (motor < 0 || motor >= NumMotors)
            throw new ArgumentOutOfRangeException(nameof(motor), "Motor must be 0 or 1");

        // Clamp speed to valid range
        speed = Math.Clamp(speed, -1.0, 1.0);

        _gpio.Write(MotorEnPin, PinValue.High);

        SoftPwmChannel pwmP, pwmN;
        if (motor == 0)
        {
            // Left motor inverted so positive speed drives forward
            pwmP = _motorPwmMapping[MotorLeftN];
            pwmN = _motorPwmMapping[MotorLeftP];
        }
        else
        {
            pwmP = _motorPwmMapping[MotorRightP];
            pwmN = _motorPwmMapping[MotorRightN];
        }

        if (speed > 0.0)
        {
            pwmP.ChangeDutyCycle(100);
            pwmN.ChangeDutyCycle(100 - (speed * 100));
        }
        else if (speed < 0.0)
        {
            pwmP.ChangeDutyCycle(100 - (-speed * 100));
            pwmN.ChangeDutyCycle(100);
        }
        else
        {
            pwmP.ChangeDutyCycle(100);
            pwmN.ChangeDutyCycle(100);
        }
    }

    public void SetMotorSpeeds(double leftSpeed, double rightSpeed)
    {
        SetMotorSpeed(MotorLeft, leftSpeed);
        SetMotorSpeed(MotorRight, rightSpeed);
    }

    public void DisableMotors()
    {
        _gpio.Write(MotorEnPin, PinValue.Low);
        _motorPwmMapping[MotorLeftP].ChangeDutyCycle(0);
        _motorPwmMapping[MotorLeftN].ChangeDutyCycle(0);
        _motorPwmMapping[MotorRightP].ChangeDutyCycle(0);
        _motorPwmMapping[MotorRightN].ChangeDutyCycle(0);
    }

    public void Forward(double speed = 1.0)
    {
        SetMotorSpeeds(speed, speed);
    }

    public void Backward(double speed = 1.0)
    {
        SetMotorSpeeds(-speed, -speed);
    }

    public void TurnLeft(double speed = 1.0)
    {
        SetMotorSpeeds(-speed, speed);
    }

    public void TurnRight(double speed = 1.0)
    {
        SetMotorSpeeds(speed, -speed);
    }

    public void CurveForwardLeft(double speed = 1.0)
    {
        SetMotorSpeeds(0.0, speed);
    }

    public void CurveForwardRight(double speed = 1.0)
    {
        SetMotorSpeeds(speed, 0.0);
    }

    public void CurveBackwardLeft(double speed = 1.0)
    {
        SetMotorSpeeds(0.0, -speed);
    }

    public void CurveBackwardRight(double speed = 1.0)
    {
        SetMotorSpeeds(-speed, 0.0);
    }

    public void Stop()
    {
        SetMotorSpeeds(0.0, 0.0);
    }

    public void Coast()
    {
        DisableMotors();
    }

    public void ShowUnderlighting()
    {
        _sn3218.Enable();
        _sn3218.Output(_underlight);  // Make sure current values are displayed
    }

    public void DisableUnderlighting()
    {
        _sn3218.Disable();
    }

    public void SetUnderlight(int light, byte r, byte g, byte b, bool show = true)
    {
        if (light < 0 || light >= NumUnderlights)
            throw new ArgumentOutOfRangeException(nameof(light), "Light must be 0-5");

        _underlight[light * 3] = r;
        _underlight[light * 3 + 1] = g;
        _underlight[light * 3 + 2] = b;
        _sn3218.Output(_underlight);

        if (show)
        {
            ShowUnderlighting();
        }
    }

    public void SetUnderlightHsv(int light, double h, double s = 1.0, double v = 1.0, bool show = true)
    {
        var rgb = ColorUtilities.HsvToRgb(h, s, v);
        SetUnderlight(light, (byte)(rgb[0] * 255), (byte)(rgb[1] * 255), (byte)(rgb[2] * 255), show);
    }

    public void FillUnderlighting(byte r, byte g, byte b, bool show = true)
    {
        for (int i = 0; i < NumUnderlights; i++)
        {
            SetUnderlight(i, r, g, b, false);
        }
        if (show)
        {
            ShowUnderlighting();
        }
    }

    public void FillUnderlightingHsv(double h, double s = 1.0, double v = 1.0, bool show = true)
    {
        var rgb = ColorUtilities.HsvToRgb(h, s, v);
        FillUnderlighting((byte)(rgb[0] * 255), (byte)(rgb[1] * 255), (byte)(rgb[2] * 255), show);
    }

    public void ClearUnderlight(int light, bool show = true)
    {
        SetUnderlight(light, 0, 0, 0, show);
    }

    public void ClearUnderlighting(bool show = true)
    {
        FillUnderlighting(0, 0, 0, show);
    }

    public void SetUnderlights(int[] lights, byte r, byte g, byte b, bool show = true)
    {
        foreach (var light in lights)
        {
            SetUnderlight(light, r, g, b, false);
        }
        if (show)
        {
            ShowUnderlighting();
        }
    }

    public void SetUnderlightsHsv(int[] lights, double h, double s = 1.0, double v = 1.0, bool show = true)
    {
        var rgb = ColorUtilities.HsvToRgb(h, s, v);
        SetUnderlights(lights, (byte)(rgb[0] * 255), (byte)(rgb[1] * 255), (byte)(rgb[2] * 255), show);
    }

    public void ClearUnderlights(int[] lights, bool show = true)
    {
        SetUnderlights(lights, 0, 0, 0, show);
    }

    public double ReadDistance(int timeout = 50, int samples = 3)
    {
        const long offset = 190000; // Time in ns the measurement takes (prevents over estimates)
        double totalDistance = 0;
        int validSamples = 0;
        var sw = new Stopwatch();
        sw.Start();

        while (validSamples < samples && sw.ElapsedMilliseconds < timeout)
        {
            _gpio.Write(UltraTrigPin, PinValue.High);
            Thread.Sleep(TimeSpan.FromMilliseconds(0.01)); // 10 microseconds
            _gpio.Write(UltraTrigPin, PinValue.Low);

            var pulseStart = DateTime.UtcNow.Ticks;
            while (_gpio.Read(UltraEchoPin) == PinValue.Low)
            {
                if ((DateTime.UtcNow.Ticks - pulseStart) / TimeSpan.TicksPerMillisecond > timeout)
                {
                    return 0;
                }
            }

            pulseStart = DateTime.UtcNow.Ticks;
            while (_gpio.Read(UltraEchoPin) == PinValue.High)
            {
                if ((DateTime.UtcNow.Ticks - pulseStart) / TimeSpan.TicksPerMillisecond > timeout)
                {
                    return 0;
                }
            }

            var pulseEnd = DateTime.UtcNow.Ticks;
            var pulseDuration = ((pulseEnd - pulseStart) * 100) - offset; // Convert ticks to nanoseconds

            if (pulseDuration > 0 && pulseDuration < timeout * 1000000)
            {
                totalDistance += pulseDuration * SpeedOfSoundCmNs / 2;
                validSamples++;
            }
        }

        return validSamples > 0 ? totalDistance / validSamples : 0;
    }

    public void InitializeServo(double minAngle = -90, double maxAngle = 90, double minPulseWidth = 0.0005, double maxPulseWidth = 0.0025)
    {
        if (_servo != null)
            throw new InvalidOperationException("Servo is already initialized.");

        _servo = new ServoMotor(_gpio, ServoPin, minAngle, maxAngle, minPulseWidth, maxPulseWidth);
    }

    public void SetServoValue(double value)
    {
        if (_servo == null)
            InitializeServo();
        
        _servo?.SetValue(value);
    }

    public void SetServoAngle(double angle)
    {
        if (_servo == null)
            InitializeServo();

        _servo?.SetAngle(angle);
    }

    public void DisableServo()
    {
        _servo?.Disable();
    }

    public void ServoToCenter()
    {
        SetServoValue(0);
    }

    public void ServoToMin()
    {
        SetServoValue(-1);
    }

    public void ServoToMax()
    {
        SetServoValue(1);
    }

    public void ServoToPercent(double value, double valueMin = 0, double valueMax = 1, double angleMin = -90, double angleMax = 90)
    {
        if (_servo == null)
            InitializeServo(angleMin, angleMax);

        var normalizedValue = (value - valueMin) / (valueMax - valueMin);
        var angle = angleMin + normalizedValue * (angleMax - angleMin);
        SetServoAngle(angle);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            DisableUnderlighting();
            DisableMotors();
            DisableServo();

            foreach (var pwm in _ledPwmMapping.Values)
            {
                pwm.Dispose();
            }

            foreach (var pwm in _motorPwmMapping.Values)
            {
                pwm.Dispose();
            }

            _servo?.Dispose();
            _sn3218.Dispose();
            _gpio.Dispose();
            _disposed = true;
        }
    }
}
