using System.Device.Gpio;
using TriloBot.Platform;

namespace TriloBot.Motor
{
    /// <summary>
    /// Manages all motor control operations for the TriloBot.
    /// </summary>
    public class MotorManager : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// The GPIO controller used for motor pin operations.
        /// </summary>
        private readonly GpioController _gpio;

        /// <summary>
        /// Mapping of motor pins to their PWM channels.
        /// </summary>
        private readonly Dictionary<int, SoftPwmChannel> _motorPwmMapping;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MotorManager"/> class and sets up all motor pins and PWM channels.
        /// </summary>
        /// <param name="gpio">The GPIO controller to use for pin operations.</param>
        public MotorManager(GpioController gpio)
        {
            _gpio = gpio;

            // Setup motor pins using Motor enum and extensions
            _gpio.OpenPin(MotorExtensions.GetEnablePin(), PinMode.Output);
            foreach (var motor in (Motor[])Enum.GetValues(typeof(Motor)))
            {
                _gpio.OpenPin(motor.GetPositivePin(), PinMode.Output);
                _gpio.OpenPin(motor.GetNegativePin(), PinMode.Output);
            }

            _motorPwmMapping = new Dictionary<int, SoftPwmChannel>
            {
                [Motor.MotorLeft.GetPositivePin()] = new SoftPwmChannel(_gpio, Motor.MotorLeft.GetPositivePin(), 100),
                [Motor.MotorLeft.GetNegativePin()] = new SoftPwmChannel(_gpio, Motor.MotorLeft.GetNegativePin(), 100),
                [Motor.MotorRight.GetPositivePin()] = new SoftPwmChannel(_gpio, Motor.MotorRight.GetPositivePin(), 100),
                [Motor.MotorRight.GetNegativePin()] = new SoftPwmChannel(_gpio, Motor.MotorRight.GetNegativePin(), 100)
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the speed and direction of a single motor.
        /// </summary>
        /// <param name="motor">The motor index (0 for left motor, 1 for right).</param>
        /// <param name="speed">Speed value between -1.0 (full reverse) and 1.0 (full forward).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the motor index is out of range.</exception>
        public void SetMotorSpeed(Motor motor, double speed)
        {
            // Clamp speed to valid range
            speed = Math.Clamp(speed, -1.0, 1.0);

            Console.WriteLine($"Setting motor {motor} speed to {speed}");

            _gpio.Write(MotorExtensions.GetEnablePin(), PinValue.High);

            // Left motor inverted, positive speed drives forward
            var pwmP = _motorPwmMapping[motor.GetPositivePin()];
            var pwmN = _motorPwmMapping[motor.GetNegativePin()];

            switch (speed)
            {
                case > 0.0:
                    pwmP.ChangeDutyCycle(100);
                    pwmN.ChangeDutyCycle(100 - (speed * 100));
                    break;
                case < 0.0:
                    pwmP.ChangeDutyCycle(100 - (-speed * 100));
                    pwmN.ChangeDutyCycle(100);
                    break;
                default:
                    pwmP.ChangeDutyCycle(100);
                    pwmN.ChangeDutyCycle(100);
                    break;
            }
        }

        /// <summary>
        /// Sets the speed and direction of both motors.
        /// </summary>
        /// <param name="leftSpeed">Speed for the left motor (-1.0 to 1.0).</param>
        /// <param name="rightSpeed">Speed for the right motor (-1.0 to 1.0).</param>
        public void SetMotorSpeeds(double leftSpeed, double rightSpeed)
        {
            SetMotorSpeed(Motor.MotorLeft, leftSpeed);
            SetMotorSpeed(Motor.MotorRight, rightSpeed);
        }

        /// <summary>
        /// Disables both motors and sets their PWM to 0.
        /// </summary>
        public void DisableMotors()
        {
            _gpio.Write(MotorExtensions.GetEnablePin(), PinValue.Low);
            foreach (var motor in (Motor[])Enum.GetValues(typeof(Motor)))
            {
                _motorPwmMapping[motor.GetPositivePin()].ChangeDutyCycle(0);
                _motorPwmMapping[motor.GetNegativePin()].ChangeDutyCycle(0);
            }
        }

        /// <summary>Drives both motors forward at the specified speed.</summary>
        /// <param name="speed">Speed value (default 0.25).</param>
        public void Forward(double speed = 1) => SetMotorSpeeds(speed, speed);

        /// <summary>Drives both motors backward at the specified speed.</summary>
        /// <param name="speed">Speed value (default 0.25).</param>
        public void Backward(double speed = 0.25) => SetMotorSpeeds(-speed, -speed);

        /// <summary>Turns the robot left in place at the specified speed.</summary>
        /// <param name="speed">Speed value (default 0.25).</param>
        public void TurnLeft(double speed = 0.25) => SetMotorSpeeds(-speed, speed);

        /// <summary>Turns the robot right in place at the specified speed.</summary>
        /// <param name="speed">Speed value (default 0.25).</param>
        public void TurnRight(double speed = 0.25) => SetMotorSpeeds(speed, -speed);

        /// <summary>Curves forward left (left motor stopped, right motor forward).</summary>
        /// <param name="speed">Speed value (default 0.25).</param>
        public void CurveForwardLeft(double speed = 0.25) => SetMotorSpeeds(0.0, speed);

        /// <summary>Curves forward right (right motor stopped, left motor forward).</summary>
        /// <param name="speed">Speed value (default 0.25).</param>
        public void CurveForwardRight(double speed = 0.25) => SetMotorSpeeds(speed, 0.0);

        /// <summary>Curves backward left (left motor stopped, right motor backward).</summary>
        /// <param name="speed">Speed value (default 0.25).</param>
        public void CurveBackwardLeft(double speed = 0.25) => SetMotorSpeeds(0.0, -speed);

        /// <summary>Curves backward right (right motor stopped, left motor backward).</summary>
        /// <param name="speed">Speed value (default 0.25).</param>
        public void CurveBackwardRight(double speed = 0.25) => SetMotorSpeeds(-speed, 0.0);

        /// <summary>Stops both motors (brake mode).</summary>
        public void Stop() => SetMotorSpeeds(0.0, 0.0);

        /// <summary>Disables both motors (coast mode).</summary>
        public void Coast() => DisableMotors();

        #endregion

        #region IDisposable Support

        /// <summary>
        /// Disposes the MotorManager, disables motors, and releases all PWM resources.
        /// </summary>
        public void Dispose()
        {
            DisableMotors();
            foreach (var pwm in _motorPwmMapping.Values)
            {
                pwm.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
