using System.Device.Gpio;
using TriloBot.Platform;

namespace TriloBot.Light
{
    /// <summary>
    /// Manages all LED and underlighting operations for the TriloBot.
    /// </summary>
    public class LightManager : IDisposable
    {
        #region Private Fields

        /// <summary>
        /// Mapping of LED pins to their PWM channels.
        /// </summary>
        private readonly Dictionary<int, SoftPwmChannel> _ledPwmMapping = [];

        /// <summary>
        /// SN3218 LED driver instance.
        /// </summary>
        private readonly Sn3218 _sn3218 = new();

        /// <summary>
        /// Buffer for underlighting RGB values.
        /// </summary>
        private readonly byte[] _underlight = new byte[18];

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LightManager"/> class.
        /// </summary>
        /// <param name="gpio">The GPIO controller to use for pin operations.</param>
        public LightManager(GpioController gpio)
        {
            var gpio1 = gpio;

            // Open LED pins for PWM control
            foreach (var pin in Enum.GetValues(typeof(Lights)).Cast<Lights>().Where(light => light.IsPinable()).Select(light => light.ToPinNumber()))
            {
                gpio1.OpenPin(pin, PinMode.Output);
                _ledPwmMapping[pin] = new SoftPwmChannel(gpio1, pin, 2000);
            }

            // Initialize SN3218 LED driver
            try
            {
                _sn3218.Output(_underlight);
                _sn3218.EnableLeds(0b111111111111111111);
                ShowUnderlighting();
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error initializing SN3218 LED driver: {ex.Message}");
                Console.WriteLine("Please check I2C connections and address");
                throw;
            }

            Console.WriteLine("LightManager initialized successfully.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the brightness of a button LED.
        /// </summary>
        /// <param name="light">The light whose LED to set.</param>
        /// <param name="value">Brightness value between 0.0 and 1.0.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if parameters are out of range.</exception>
        public void SetButtonLed(Lights light, double value)
        {
            _ledPwmMapping[light.ToPinNumber()].ChangeDutyCycle(value * 100.0);
        }

        /// <summary>
        /// Enables and displays the current underlighting values.
        /// </summary>
        public void ShowUnderlighting()
        {
            try
            {
                _sn3218.Enable();
                _sn3218.Output(_underlight);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error initializing SN3218 LED driver: {ex.Message}");
                Console.WriteLine("Please check I2C connections and address");
            }
        }

        /// <summary>
        /// Disables the underlighting.
        /// </summary>
        public void DisableUnderlighting()
        {
            _sn3218.Disable();
        }

        /// <summary>
        /// Sets the RGB value of a single underlight.
        /// </summary>
        /// <param name="light">The underlight to set.</param>
        /// <param name="r">Red value (0-255).</param>
        /// <param name="g">Green value (0-255).</param>
        /// <param name="b">Blue value (0-255).</param>
        /// <param name="show">Whether to immediately update the lights.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if light is out of range.</exception>
        public void SetUnderlight(Lights light, byte r, byte g, byte b, bool show = true)
        {
            int index = (int)light;
            _underlight[index * 3] = r;
            _underlight[index * 3 + 1] = g;
            _underlight[index * 3 + 2] = b;
            _sn3218.Output(_underlight);

            if (show)
            {
                ShowUnderlighting();
            }
        }

        /// <summary>
        /// Sets the HSV value of a single underlight.
        /// </summary>
        /// <param name="light">The underlight to set.</param>
        /// <param name="h">Hue value.</param>
        /// <param name="s">Saturation value.</param>
        /// <param name="v">Value (brightness).</param>
        /// <param name="show">Whether to immediately update the lights.</param>
        public void SetUnderlightHsv(Lights light, double h, double s = 1.0, double v = 1.0, bool show = true)
        {
            var rgb = ColorUtilities.HsvToRgb(h, s, v);
            SetUnderlight(light, (byte)(rgb[0] * 255), (byte)(rgb[1] * 255), (byte)(rgb[2] * 255), show);
        }

        /// <summary>
        /// Fills all underlights with the specified RGB color.
        /// </summary>
        /// <param name="r">Red value (0-255).</param>
        /// <param name="g">Green value (0-255).</param>
        /// <param name="b">Blue value (0-255).</param>
        /// <param name="show">Whether to immediately update the lights.</param>
        public void FillUnderlighting(byte r, byte g, byte b, bool show = true)
        {
            foreach (var light in LightsExtensions.LightsLeft.Concat(LightsExtensions.LightsRight))
            {
                SetUnderlight(light, r, g, b, false);
            }

            if (show)
            {
                ShowUnderlighting();
            }
        }

        /// <summary>
        /// Fills all underlights with the specified HSV color.
        /// </summary>
        /// <param name="h">Hue value.</param>
        /// <param name="s">Saturation value.</param>
        /// <param name="v">Value (brightness).</param>
        /// <param name="show">Whether to immediately update the lights.</param>
        public void FillUnderlightingHsv(double h, double s = 1.0, double v = 1.0, bool show = true)
        {
            var rgb = ColorUtilities.HsvToRgb(h, s, v);
            FillUnderlighting((byte)(rgb[0] * 255), (byte)(rgb[1] * 255), (byte)(rgb[2] * 255), show);
        }

        /// <summary>
        /// Clears a single underlight (sets it to off).
        /// </summary>
        /// <param name="light">The index of the underlight (0-5).</param>
        /// <param name="show">Whether to immediately update the lights.</param>
        public void ClearUnderlight(Lights light, bool show = true)
        {
            SetUnderlight(light, 0, 0, 0, show);
        }

        /// <summary>
        /// Clears all underlights (sets them to off).
        /// </summary>
        /// <param name="show">Whether to immediately update the lights.</param>
        public void ClearUnderlighting(bool show = true)
        {
            FillUnderlighting(0, 0, 0, show);
        }

        /// <summary>
        /// Sets the RGB value for multiple underlights.
        /// </summary>
        /// <param name="lights">Array of underlight indices.</param>
        /// <param name="r">Red value (0-255).</param>
        /// <param name="g">Green value (0-255).</param>
        /// <param name="b">Blue value (0-255).</param>
        /// <param name="show">Whether to immediately update the lights.</param>
        public void SetUnderlights(Lights[] lights, byte r, byte g, byte b, bool show = true)
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

        /// <summary>
        /// Sets the HSV value for multiple underlights.
        /// </summary>
        /// <param name="lights">Array of underlight indices.</param>
        /// <param name="h">Hue value.</param>
        /// <param name="s">Saturation value.</param>
        /// <param name="v">Value (brightness).</param>
        /// <param name="show">Whether to immediately update the lights.</param>
        public void SetUnderlightsHsv(Lights[] lights, double h, double s = 1.0, double v = 1.0, bool show = true)
        {
            var rgb = ColorUtilities.HsvToRgb(h, s, v);
            SetUnderlights(lights, (byte)(rgb[0] * 255), (byte)(rgb[1] * 255), (byte)(rgb[2] * 255), show);
        }

        /// <summary>
        /// Clears multiple underlights (sets them to off).
        /// </summary>
        /// <param name="lights">Array of underlight indices.</param>
        /// <param name="show">Whether to immediately update the lights.</param>
        public void ClearUnderlights(Lights[] lights, bool show = true)
        {
            SetUnderlights(lights, 0, 0, 0, show);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Disposes the LightManager and releases all resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var pwm in _ledPwmMapping.Values)
            {
                pwm.Dispose();
            }
            _sn3218.Dispose();

            GC.SuppressFinalize(this);
        }
    }

    #endregion
}
