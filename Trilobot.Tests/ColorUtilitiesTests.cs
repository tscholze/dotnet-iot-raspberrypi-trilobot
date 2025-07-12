using Xunit;
using TriloBot.Light;

namespace Trilobot.Tests
{
    public class ColorUtilitiesTests
    {
        [Fact]
        public void HsvToRgb_ShouldReturnGray_WhenSaturationIsZero()
        {
            // Arrange
            double h = 0.5, s = 0.0, v = 0.5;

            // Act
            var result = ColorUtilities.HsvToRgb(h, s, v);

            // Assert
            Assert.Equal([0.5, 0.5, 0.5], result);
        }

        [Fact]
        public void HsvToRgb_ShouldReturnRed_WhenHueIsZero()
        {
            // Arrange
            double h = 0.0, s = 1.0, v = 1.0;

            // Act
            var result = ColorUtilities.HsvToRgb(h, s, v);

            // Assert
            Assert.Equal([1.0, 0.0, 0.0], result);
        }

        [Fact]
        public void HsvToRgb_ShouldReturnBlue_WhenHueIsTwoThirds()
        {
            // Arrange
            double h = 2.0 / 3.0, s = 1.0, v = 1.0;

            // Act
            var result = ColorUtilities.HsvToRgb(h, s, v);

            // Assert
            Assert.Equal([0.0, 0.0, 1.0], result);
        }
    }
}
