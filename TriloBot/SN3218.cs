using System;
using System.Device.I2c;

namespace TriloBot;

/// <summary>
/// Driver for the SN3218 18-channel LED driver
/// </summary>
public class SN3218 : IDisposable
{
    private const byte DefaultI2cAddress = 0x54;  // SN3218 default address
    private const int DefaultI2cBus = 1;  // Raspberry Pi default I2C bus
    private const byte CMD_ENABLE_OUTPUT = 0x00;
    private const byte CMD_SET_PWM_VALUES = 0x01;
    private const byte CMD_ENABLE_LEDS = 0x13;
    private const byte CMD_UPDATE = 0x16;
    private const byte CMD_RESET = 0x17;

    private readonly I2cDevice _device;
    private bool _disposed;
    private readonly byte[][] _channelGammaTables;

    public SN3218(int busId = DefaultI2cBus, uint enableMask = 0b111111111111111111)
    {
        var settings = new I2cConnectionSettings(busId, DefaultI2cAddress);
        _device = I2cDevice.Create(settings);
        _disposed = false;

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

        Initialize(enableMask);
    }

    private void Initialize(uint enableMask)
    {
        Reset();
        Enable();
        EnableLeds(enableMask);
    }

    public void Reset()
    {
        _device.Write(new byte[] { CMD_RESET, 0xFF });
    }

    public void Enable()
    {
        _device.Write(new byte[] { CMD_ENABLE_OUTPUT, 0x01 });
    }

    public void Disable()
    {
        _device.Write(new byte[] { CMD_ENABLE_OUTPUT, 0x00 });
    }

    public void EnableLeds(uint mask)
    {
        _device.Write(new[] { 
            CMD_ENABLE_LEDS, 
            (byte)(mask & 0x3F),
            (byte)((mask >> 6) & 0x3F),
            (byte)((mask >> 12) & 0x3F)
        });
        _device.Write(new byte[] { CMD_UPDATE, 0xFF });
    }

    public void Output(byte[] values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        if (values.Length != 18)
            throw new ArgumentException("Values array must contain exactly 18 values", nameof(values));

        var correctedValues = new byte[18];
        for (int i = 0; i < 18; i++)
        {
            correctedValues[i] = _channelGammaTables[i][values[i]];
        }

        OutputRaw(correctedValues);
    }

    public void OutputRaw(byte[] values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        if (values.Length != 18)
            throw new ArgumentException("Values array must contain exactly 18 values", nameof(values));

        var data = new byte[19];
        data[0] = CMD_SET_PWM_VALUES;
        Array.Copy(values, 0, data, 1, 18);
        _device.Write(data);

        // Update the output
        _device.Write(new byte[] { CMD_UPDATE, 0xFF });
    }

    public void SetChannelGamma(int channel, byte[] gammaTable)
    {
        if (channel < 0 || channel >= 18)
            throw new ArgumentOutOfRangeException(nameof(channel), "Channel must be between 0 and 17");

        if (gammaTable == null)
            throw new ArgumentNullException(nameof(gammaTable));

        if (gammaTable.Length != 256)
            throw new ArgumentException("Gamma table must contain exactly 256 values", nameof(gammaTable));

        Array.Copy(gammaTable, _channelGammaTables[channel], 256);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _device?.Dispose();
            _disposed = true;
        }
    }
}
