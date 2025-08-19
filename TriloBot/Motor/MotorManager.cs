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
        /// <param name="motor">The motor to control (MotorLeft or MotorRight).</param>
        /// <param name="speed">Speed value between -1.0 (full reverse) and 1.0 (full forward). Positive values drive the motor forward, negative values drive it in reverse.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the speed is outside the valid range of -1.0 to 1.0.</exception>
        /// <remarks>
        /// The left motor has inverted polarity compared to the right motor to ensure both motors move in the same direction when given the same speed value.
        /// </remarks>
        public void SetMotorSpeed(Motor motor, double speed)
        {
            // Validate speed range.
            if (Math.Abs(speed) > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), "Speed must be between -1.0 and 1.0");
            }

            // Clamp speed to valid range
            _gpio.Write(MotorExtensions.GetEnablePin(), PinValue.High);

            // Left motor inverted, positive speed drives forward
            var pwmP = motor == Motor.MotorLeft ? _motorPwmMapping[Motor.MotorLeft.GetNegativePin()] : _motorPwmMapping[Motor.MotorRight.GetPositivePin()];
            var pwmN = motor == Motor.MotorLeft ? _motorPwmMapping[Motor.MotorLeft.GetPositivePin()] : _motorPwmMapping[Motor.MotorRight.GetNegativePin()];

            switch (speed)
            {
                case > 0.0:
                    pwmP.ChangeDutyCycle(100);
                    pwmN.ChangeDutyCycle(100 - speed * 100);
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
        /// Sets the speed and direction of both motors simultaneously.
        /// </summary>
        /// <param name="leftSpeed">Speed for the left motor in range -1.0 to 1.0. Positive values drive forward, negative values drive in reverse.</param>
        /// <param name="rightSpeed">Speed for the right motor in range -1.0 to 1.0. Positive values drive forward, negative values drive in reverse.</param>
        /// <remarks>
        /// This method is the foundation for all movement operations. Different speed values for left and right motors enable turning.
        /// </remarks>
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

        /// <summary>
        /// Moves the robot forward at the specified speed.
        /// </summary>
        /// <param name="speed">Forward movement speed in range 0.0 to 1.0, where 1.0 represents maximum speed.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when speed is not between 0.0 and 1.0.</exception>
        /// <remarks>
        /// Both motors rotate at the same positive speed to achieve forward movement.
        /// </remarks>
        public void Forward(double speed) => SetMotorSpeeds(speed, speed);

        /// <summary>
        /// Moves the robot backward at the specified speed.
        /// </summary>
        /// <param name="speed">Backward movement speed in range 0.0 to 1.0, where 1.0 represents maximum speed.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when speed is not between 0.0 and 1.0.</exception>
        /// <remarks>
        /// Both motors rotate in reverse at the specified speed to achieve backward movement.
        /// </remarks>
        public void Backward(double speed) => SetMotorSpeeds(-speed, -speed);

        /// <summary>
        /// Turns the robot left in place at the specified speed.
        /// </summary>
        /// <param name="speed">Turn speed in range 0.0 to 1.0, where 1.0 represents maximum turning speed.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when speed is not between 0.0 and 1.0.</exception>
        /// <remarks>
        /// Left motor rotates in reverse while right motor rotates forward, causing the robot to pivot left.
        /// </remarks>
        public void TurnLeft(double speed) => SetMotorSpeeds(-speed, speed);

        /// <summary>
        /// Turns the robot right in place at the specified speed.
        /// </summary>
        /// <param name="speed">Turn speed in range 0.0 to 1.0, where 1.0 represents maximum turning speed.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when speed is not between 0.0 and 1.0.</exception>
        /// <remarks>
        /// Left motor rotates forward while right motor rotates in reverse, causing the robot to pivot right.
        /// </remarks>
        public void TurnRight(double speed) => SetMotorSpeeds(speed, -speed);

        /// <summary>
        /// Curves the robot forward and to the left by stopping the left motor and driving the right motor.
        /// </summary>
        /// <param name="speed">Forward speed in range 0.0 to 1.0, where 1.0 represents maximum speed.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when speed is not between 0.0 and 1.0.</exception>
        /// <remarks>
        /// Creates a wide turning arc to the left by keeping the left motor stationary while the right motor drives forward.
        /// </remarks>
        public void CurveForwardLeft(double speed) => SetMotorSpeeds(0.0, speed);

        /// <summary>
        /// Curves the robot forward and to the right by stopping the right motor and driving the left motor.
        /// </summary>
        /// <param name="speed">Forward speed in range 0.0 to 1.0, where 1.0 represents maximum speed.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when speed is not between 0.0 and 1.0.</exception>
        /// <remarks>
        /// Creates a wide turning arc to the right by keeping the right motor stationary while the left motor drives forward.
        /// </remarks>
        public void CurveForwardRight(double speed) => SetMotorSpeeds(speed, 0.0);

        /// <summary>
        /// Curves backward left (left motor stopped, right motor backward).
        /// </summary>
        /// <param name="speed">Speed value in range -1.0 to 1.0.</param>
        public void CurveBackwardLeft(double speed) => SetMotorSpeeds(0.0, -speed);

        /// <summary>
        /// Curves backward right (right motor stopped, left motor backward).
        /// </summary>
        /// <param name="speed">Speed value in range -1.0 to 1.0.</param>
        public void CurveBackwardRight(double speed) => SetMotorSpeeds(-speed, 0.0);

        /// <summary>
        /// Stops both motors immediately by setting their speeds to zero.
        /// </summary>
        /// <remarks>
        /// This method provides an immediate halt to all motor movement and is safe to call at any time.
        /// </remarks>
        public void Stop() => SetMotorSpeeds(0.0, 0.0);

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
