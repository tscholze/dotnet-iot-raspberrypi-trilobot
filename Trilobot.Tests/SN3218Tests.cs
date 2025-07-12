using System;
using System.Device.I2c;
using Moq;
using Xunit;
using TriloBot.Light;

namespace Trilobot.Tests
{
    public class SN3218Tests
    {
        [Fact]
        public void Constructor_ShouldInitializeWithoutException()
        {
            // Act
            using var sn3218 = new SN3218();

            // Assert
            Assert.NotNull(sn3218);
        }

        [Fact]
        public void Reset_ShouldNotThrowException()
        {
            // Arrange
            using var sn3218 = new SN3218();

            // Act & Assert
            var exception = Record.Exception(() => sn3218.Reset());
            Assert.Null(exception);
        }

        [Fact]
        public void Enable_ShouldNotThrowException()
        {
            // Arrange
            using var sn3218 = new SN3218(I2cDevice.Create(new I2cConnectionSettings(1, 0x54)
);

            // Act & Assert
            var exception = Record.Exception(() => sn3218.Enable());
            Assert.Null(exception);
        }

        [Fact]
        public void Disable_ShouldNotThrowException()
        {
            // Arrange
            using var sn3218 = new SN3218(I2cDevice.Create(new I2cConnectionSettings(1, 0x54)
);

            // Act & Assert
            var exception = Record.Exception(() => sn3218.Disable());
            Assert.Null(exception);
        }

        [Fact]
        public void EnableLeds_ShouldNotThrowException()
        {
            // Arrange
            using var sn3218 = new SN3218();

            // Act & Assert
            var exception = Record.Exception(() => sn3218.EnableLeds(0b111111111111111111));
            Assert.Null(exception);
        }

        [Fact]
        public void Output_ShouldThrowException_WhenValuesAreNull()
        {
            // Arrange
            using var sn3218 = new SN3218();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => sn3218.Output(null));
        }

        [Fact]
        public void Output_ShouldThrowException_WhenValuesAreNotLength18()
        {
            // Arrange
            using var sn3218 = new SN3218();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => sn3218.Output(new byte[10]));
        }

        [Fact]
        public void OutputRaw_ShouldThrowException_WhenValuesAreNull()
        {
            // Arrange
            using var sn3218 = new SN3218();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => sn3218.OutputRaw(null));
        }

        [Fact]
        public void OutputRaw_ShouldThrowException_WhenValuesAreNotLength18()
        {
            // Arrange
            using var sn3218 = new SN3218();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => sn3218.OutputRaw(new byte[10]));
        }

        [Fact]
        public void Constructor_ShouldInitializeDeviceAndEnableLeds()
        {
            // Arrange
            var mockI2cDevice = new Mock<I2cDevice>();

            // Act
            using var sn3218 = new SN3218(mockI2cDevice.Object);

            // Assert
            mockI2cDevice.Verify(d => d.Write(It.Is<byte[]>(b => b[0] == 0x17 && b[1] == 0xFF)), Times.Once); // Reset
            mockI2cDevice.Verify(d => d.Write(It.Is<byte[]>(b => b[0] == 0x00 && b[1] == 0x01)), Times.Once); // Enable
            mockI2cDevice.Verify(d => d.Write(It.Is<byte[]>(b => b[0] == 0x13)), Times.Once); // Enable LEDs
        }

        [Fact]
        public void Output_ShouldApplyGammaCorrectionAndWriteValues()
        {
            // Arrange
            var mockI2cDevice = new Mock<I2cDevice>();
            using var sn3218 = new SN3218(mockI2cDevice.Object);
            var values = new byte[18];
            for (int i = 0; i < 18; i++)
            {
                values[i] = (byte)(i * 14); // Example values
            }

            // Act
            sn3218.Output(values);

            // Assert
            mockI2cDevice.Verify(d => d.Write(It.Is<byte[]>(b => b[0] == 0x01)), Times.Once); // Set PWM values
            mockI2cDevice.Verify(d => d.Write(It.Is<byte[]>(b => b[0] == 0x16 && b[1] == 0xFF)), Times.Once); // Update
        }

        [Fact]
        public void Dispose_ShouldDisposeI2cDevice()
        {
            // Arrange
            var mockI2cDevice = new Mock<I2cDevice>();
            using var sn3218 = new SN3218(mockI2cDevice.Object);

            // Act
            sn3218.Dispose();

            // Assert
            mockI2cDevice.Verify(d => d.Dispose(), Times.Once);
        }
    }
}
