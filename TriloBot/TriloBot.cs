using System.Device.Gpio;
using TriloBot.Light;
using TriloBot.Motor;

namespace TriloBot;

public class TriloBot : IDisposable
{
    // Pin definitions






    // Ultrasound sensor pins
    private const int UltraTrigPin = 13;
    private const int UltraEchoPin = 25;

    private const int ServoPin = 12;

    // Speed of sound in cm/ns
    private const double SpeedOfSoundCmNs = 343 * 100.0 / 1E9; // 0.0000343 cm/ns

    private readonly GpioController _gpio;
    private readonly Button.ButtonManager _buttonManager = null!;
    private readonly Light.LightManager _lightManager = null!;
    private readonly Motor.MotorManager _motorManager = null!;

    private readonly Ultrasound.UltrasoundManager _ultrasoundManager = null!;

    private readonly SN3218 _sn3218 = null!;
    private readonly byte[] _underlight = null!;
    private ServoMotor? _servo;
    private bool _disposed;

    public TriloBot()
    {
        _gpio = new GpioController();
        _disposed = false;

        // Setup button manager
        _buttonManager = new Button.ButtonManager(_gpio);

        // Setup light manager
        _lightManager = new Light.LightManager(_gpio);

        // Setup motor manager
        _motorManager = new Motor.MotorManager(_gpio);

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
        // Setup ultrasound manager
        _ultrasoundManager = new Ultrasound.UltrasoundManager(_gpio);
    }

    public bool ReadButton(int button) => _buttonManager.ReadButton(button);

    public void SetButtonLed(int buttonLed, double value) => _lightManager.SetButtonLed(buttonLed, value);

    // Motor control methods are now delegated to MotorManager
    public void SetMotorSpeed(int motor, double speed) => _motorManager.SetMotorSpeed(motor, speed);
    public void SetMotorSpeeds(double leftSpeed, double rightSpeed) => _motorManager.SetMotorSpeeds(leftSpeed, rightSpeed);
    public void DisableMotors() => _motorManager.DisableMotors();
    public void Forward(double speed = 1.0) => _motorManager.Forward(speed);
    public void Backward(double speed = 1.0) => _motorManager.Backward(speed);
    public void TurnLeft(double speed = 1.0) => _motorManager.TurnLeft(speed);
    public void TurnRight(double speed = 1.0) => _motorManager.TurnRight(speed);
    public void CurveForwardLeft(double speed = 1.0) => _motorManager.CurveForwardLeft(speed);
    public void CurveForwardRight(double speed = 1.0) => _motorManager.CurveForwardRight(speed);
    public void CurveBackwardLeft(double speed = 1.0) => _motorManager.CurveBackwardLeft(speed);
    public void CurveBackwardRight(double speed = 1.0) => _motorManager.CurveBackwardRight(speed);
    public void Stop() => _motorManager.Stop();
    public void Coast() => _motorManager.Coast();

    public void ShowUnderlighting() => _lightManager.ShowUnderlighting();
    public void DisableUnderlighting() => _lightManager.DisableUnderlighting();
    public void SetUnderlight(int light, byte r, byte g, byte b, bool show = true) => _lightManager.SetUnderlight(light, r, g, b, show);
    public void SetUnderlightHsv(int light, double h, double s = 1.0, double v = 1.0, bool show = true) => _lightManager.SetUnderlightHsv(light, h, s, v, show);
    public void FillUnderlighting(byte r, byte g, byte b, bool show = true) => _lightManager.FillUnderlighting(r, g, b, show);
    public void FillUnderlightingHsv(double h, double s = 1.0, double v = 1.0, bool show = true) => _lightManager.FillUnderlightingHsv(h, s, v, show);
    public void ClearUnderlight(int light, bool show = true) => _lightManager.ClearUnderlight(light, show);
    public void ClearUnderlighting(bool show = true) => _lightManager.ClearUnderlighting(show);
    public void SetUnderlights(int[] lights, byte r, byte g, byte b, bool show = true) => _lightManager.SetUnderlights(lights, r, g, b, show);
    public void SetUnderlightsHsv(int[] lights, double h, double s = 1.0, double v = 1.0, bool show = true) => _lightManager.SetUnderlightsHsv(lights, h, s, v, show);
    public void ClearUnderlights(int[] lights, bool show = true) => _lightManager.ClearUnderlights(lights, show);

    public double ReadDistance(int timeout = 50, int samples = 3)
        => _ultrasoundManager.ReadDistance();


    public void SetServoValue(double value)
    {
        if (_servo == null)
        {
            InitializeServo();
        }

        _servo?.SetValue(value);
    }

    public void SetServoAngle(double angle)
    {
        if (_servo == null)
        {
            InitializeServo();
        }

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
        {
            InitializeServo(angleMin, angleMax);
        }

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

            _servo?.Dispose();
            _motorManager?.Dispose();
            _buttonManager?.Dispose();
            _lightManager?.Dispose();
            _gpio.Dispose();
            _disposed = true;
        }
    }
}
