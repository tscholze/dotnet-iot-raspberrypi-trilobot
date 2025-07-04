using System.Device.Gpio;

namespace TriloBot.Button
{
    /// <summary>
    /// Manages button input operations for the TriloBot.
    /// </summary>
    public class ButtonManager : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// The GPIO controller used for button pin operations.
        /// </summary>
        private readonly GpioController _gpio;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ButtonManager"/> class and opens all button pins.
        /// </summary>
        /// <param name="gpio">The GPIO controller to use for pin operations.</param>
        public ButtonManager(GpioController gpio)
        {
            _gpio = gpio;

            foreach (var pin in ButtonConfigurations.ButtonPins)
            {
                _gpio.OpenPin(pin, PinMode.InputPullUp);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Reads the state of a button.
        /// </summary>
        /// <param name="button">The index of the button (0-3).</param>
        /// <returns>True if the button is pressed, otherwise false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if button index is out of range.</exception>
        public bool ReadButton(int button)
        {
            if (button < 0 || button >= ButtonConfigurations.NumberOfButtons)
            {
                throw new ArgumentOutOfRangeException(nameof(button), "Button must be 0-3");
            }

            return _gpio.Read(ButtonConfigurations.ButtonPins[button]) == PinValue.Low;
        }

        #endregion

        #region IDisposable Support

        /// <summary>
        /// Disposes the ButtonManager and closes all button pins.
        /// </summary>
        public void Dispose()
        {
            foreach (var pin in ButtonConfigurations.ButtonPins)
            {
                _gpio.ClosePin(pin);
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
