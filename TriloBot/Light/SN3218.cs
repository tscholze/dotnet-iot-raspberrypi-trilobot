using System.Device.I2c;

namespace TriloBot.Light;

/// <summary>
/// Driver for the SN3218 18-channel LED driver.
/// </summary>
internal class Sn3218 : IDisposable
{
    #region Private Constants

    /// <summary>Command to enable output.</summary>
    private const byte CommandEnableOutput = 0x00;

    /// <summary>Command to set PWM values.</summary>
    private const byte CommandSetPwmValues = 0x01;

    /// <summary>Command to enable LEDs.</summary>
    private const byte CommandEnableLeds = 0x13;

    /// <summary>Command to update output.</summary>
    private const byte CommandUpdate = 0x16;

    /// <summary>Command to reset the device.</summary>
    private const byte CommandReset = 0x17;

    #endregion

    #region Private Fields
    /// <summary>
    /// The I2C device used for communication with the SN3218.
    /// </summary>
    private readonly I2cDevice _device;

    /// <summary>
    /// Gamma correction tables for each channel.
    /// </summary>
    private readonly byte[][] _channelGammaTables;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Sn3218"/> class and sets up the device and gamma tables.
    /// </summary>
    public Sn3218()
    {
        _device = I2cDevice.Create(new I2cConnectionSettings(1, 0x54));

        // Generate default gamma table
        var defaultGammaTable = new byte[256];
        for (int i = 0; i < 256; i++)
        {
            defaultGammaTable[i] = (byte)Math.Min(255, Math.Pow(i, 2.5) / Math.Pow(255, 1.5));
        }

        // Initialize gamma tables for each channel
        _channelGammaTables = new byte[18][];
        for (int i = 0; i < 18; i++)
        {
            _channelGammaTables[i] = new byte[256];
            Array.Copy(defaultGammaTable, _channelGammaTables[i], 256);
        }

        Reset();
        Enable();
        EnableLeds(0b111111111111111111);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sends a reset command to the SN3218 device.
    /// </summary>
    public void Reset()
    {
        _device.Write([CommandReset, 0xFF]);
    }

    /// <summary>
    /// Enables the SN3218 output.
    /// </summary>
    public void Enable()
    {
        _device.Write([CommandEnableOutput, 0x01]);
    }

    /// <summary>
    /// Disables the SN3218 output.
    /// </summary>
    public void Disable()
    {
        _device.Write([CommandEnableOutput, 0x00]);
    }

    /// <summary>
    /// Enables specific LEDs using a bitmask.
    /// </summary>
    /// <param name="mask">Bitmask for which LEDs to enable (18 bits).</param>
    public void EnableLeds(uint mask)
    {
        _device.Write([
            CommandEnableLeds,
            (byte)(mask & 0x3F),
            (byte)((mask >> 6) & 0x3F),
            (byte)((mask >> 12) & 0x3F)
        ]);
        _device.Write([CommandUpdate, 0xFF]);
    }

    /// <summary>
    /// Sets the output values for all 18 channels, applying gamma correction.
    /// </summary>
    /// <param name="values">Array of 18 brightness values (0-255).</param>
    /// <exception cref="ArgumentNullException">Thrown if values is null.</exception>
    /// <exception cref="ArgumentException">Thrown if values is not length 18.</exception>
    public void Output(byte[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        if (values.Length != 18)
        {
            throw new ArgumentException("Values array must contain exactly 18 values", nameof(values));
        }

        var correctedValues = new byte[18];
        for (int i = 0; i < 18; i++)
        {
            correctedValues[i] = _channelGammaTables[i][values[i]];
        }

        OutputRaw(correctedValues);
    }

    /// <summary>
    /// Sets the gamma correction table for a specific channel.
    /// </summary>
    /// <param name="channel">Channel index (0-17).</param>
    /// <param name="gammaTable">Gamma table (256 values).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the channel is out of range.</exception>
    /// <exception cref="ArgumentNullException">Thrown if gammaTable is null.</exception>
    /// <exception cref="ArgumentException">Thrown if gammaTable is not length 256.</exception>
    public void SetChannelGamma(int channel, byte[] gammaTable)
    {
        if (channel < 0 || channel >= 18)
        {
            throw new ArgumentOutOfRangeException(nameof(channel), "Channel must be between 0 and 17");
        }

        if (gammaTable == null)
        {
            throw new ArgumentNullException(nameof(gammaTable));
        }

        if (gammaTable.Length != 256)
        {
            throw new ArgumentException("Gamma table must contain exactly 256 values", nameof(gammaTable));
        }

        Array.Copy(gammaTable, _channelGammaTables[channel], 256);
    }

    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Sets the output values for all 18 channels without gamma correction.
    /// </summary>
    /// <param name="values">Array of 18 brightness values (0-255).</param>
    /// <exception cref="ArgumentNullException">Thrown if values is null.</exception>
    /// <exception cref="ArgumentException">Thrown if values is not length 18.</exception>
    private void OutputRaw(byte[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length != 18)
        {
            throw new ArgumentException("Values array must contain exactly 18 values", nameof(values));
        }

        var data = new byte[19];
        data[0] = CommandSetPwmValues;
        Array.Copy(values, 0, data, 1, 18);
        _device.Write(data);

        // Update the output
        _device.Write([CommandUpdate, 0xFF]);
    }
    
    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the SN3218 device and releases resources.
    /// </summary>
    public void Dispose()
    {
        _device.Dispose();
    }

    #endregion
}
