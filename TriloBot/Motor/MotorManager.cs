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

            // Setup motor pins
            _gpio.OpenPin(MotorConfigurations.MotorEnPin, PinMode.Output);
            _gpio.OpenPin(MotorConfigurations.MotorLeftP, PinMode.Output);
            _gpio.OpenPin(MotorConfigurations.MotorLeftN, PinMode.Output);
            _gpio.OpenPin(MotorConfigurations.MotorRightP, PinMode.Output);
            _gpio.OpenPin(MotorConfigurations.MotorRightN, PinMode.Output);

            _motorPwmMapping = new Dictionary<int, SoftPwmChannel>
            {
                [MotorConfigurations.MotorLeftP] = new SoftPwmChannel(_gpio, MotorConfigurations.MotorLeftP, 100),
                [MotorConfigurations.MotorLeftN] = new SoftPwmChannel(_gpio, MotorConfigurations.MotorLeftN, 100),
                [MotorConfigurations.MotorRightP] = new SoftPwmChannel(_gpio, MotorConfigurations.MotorRightP, 100),
                [MotorConfigurations.MotorRightN] = new SoftPwmChannel(_gpio, MotorConfigurations.MotorRightN, 100)
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the speed and direction of a single motor.
        /// </summary>
        /// <param name="motor">The motor index (0 for left, 1 for right).</param>
        /// <param name="speed">Speed value between -1.0 (full reverse) and 1.0 (full forward).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if motor index is out of range.</exception>
        public void SetMotorSpeed(int motor, double speed)
        {
            if (motor < 0 || motor >= MotorConfigurations.NumMotors)
            {
                throw new ArgumentOutOfRangeException(nameof(motor), "Motor must be 0 or 1");
            }

            // Clamp speed to valid range
            speed = Math.Clamp(speed, -1.0, 1.0);

            _gpio.Write(MotorConfigurations.MotorEnPin, PinValue.High);

            SoftPwmChannel pwmP, pwmN;
            if (motor == 0)
            {
                // Left motor inverted so positive speed drives forward
                pwmP = _motorPwmMapping[MotorConfigurations.MotorLeftN];
                pwmN = _motorPwmMapping[MotorConfigurations.MotorLeftP];
            }
            else
            {
                pwmP = _motorPwmMapping[MotorConfigurations.MotorRightP];
                pwmN = _motorPwmMapping[MotorConfigurations.MotorRightN];
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

        /// <summary>
        /// Sets the speed and direction of both motors.
        /// </summary>
        /// <param name="leftSpeed">Speed for the left motor (-1.0 to 1.0).</param>
        /// <param name="rightSpeed">Speed for the right motor (-1.0 to 1.0).</param>
        public void SetMotorSpeeds(double leftSpeed, double rightSpeed)
        {
            SetMotorSpeed(MotorConfigurations.MotorLeft, leftSpeed);
            SetMotorSpeed(MotorConfigurations.MotorRight, rightSpeed);
        }

        /// <summary>
        /// Disables both motors and sets their PWM to 0.
        /// </summary>
        public void DisableMotors()
        {
            _gpio.Write(MotorConfigurations.MotorEnPin, PinValue.Low);
            _motorPwmMapping[MotorConfigurations.MotorLeftP].ChangeDutyCycle(0);
            _motorPwmMapping[MotorConfigurations.MotorLeftN].ChangeDutyCycle(0);
            _motorPwmMapping[MotorConfigurations.MotorRightP].ChangeDutyCycle(0);
            _motorPwmMapping[MotorConfigurations.MotorRightN].ChangeDutyCycle(0);
        }

        /// <summary>Drives both motors forward at the specified speed.</summary>
        /// <param name="speed">Speed value (default 1.0).</param>
        public void Forward(double speed = 1.0) => SetMotorSpeeds(speed, speed);

        /// <summary>Drives both motors backward at the specified speed.</summary>
        /// <param name="speed">Speed value (default 1.0).</param>
        public void Backward(double speed = 1.0) => SetMotorSpeeds(-speed, -speed);
       
        /// <summary>Turns the robot left in place at the specified speed.</summary>
        /// <param name="speed">Speed value (default 1.0).</param>
        public void TurnLeft(double speed = 1.0) => SetMotorSpeeds(-speed, speed);
       
        /// <summary>Turns the robot right in place at the specified speed.</summary>
        /// <param name="speed">Speed value (default 1.0).</param>
        public void TurnRight(double speed = 1.0) => SetMotorSpeeds(speed, -speed);
       
        /// <summary>Curves forward left (left motor stopped, right motor forward).</summary>
        /// <param name="speed">Speed value (default 1.0).</param>
        public void CurveForwardLeft(double speed = 1.0) => SetMotorSpeeds(0.0, speed);
       
        /// <summary>Curves forward right (right motor stopped, left motor forward).</summary>
        /// <param name="speed">Speed value (default 1.0).</param>
        public void CurveForwardRight(double speed = 1.0) => SetMotorSpeeds(speed, 0.0);
       
        /// <summary>Curves backward left (left motor stopped, right motor backward).</summary>
        /// <param name="speed">Speed value (default 1.0).</param>
        public void CurveBackwardLeft(double speed = 1.0) => SetMotorSpeeds(0.0, -speed);
       
        /// <summary>Curves backward right (right motor stopped, left motor backward).</summary>
        /// <param name="speed">Speed value (default 1.0).</param>
        public void CurveBackwardRight(double speed = 1.0) => SetMotorSpeeds(-speed, 0.0);
       
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
