namespace TriloBot.Light;

/// <summary>
/// Provides color conversion utilities
/// </summary>
public static class ColorUtilities
{
    /// <summary>
    /// Converts HSV color values to RGB
    /// </summary>
    /// <param name="h">Hue value between 0 and 1</param>
    /// <param name="s">Saturation value between 0 and 1</param>
    /// <param name="v">Value (brightness) between 0 and 1</param>
    /// <returns>Array containing [r, g, b] values between 0 and 1</returns>
    public static double[] HsvToRgb(double h, double s, double v)
    {
        if (s <= 0.0)
        {
            return [v, v, v];
        }

        double hh = h * 6.0;
        if (hh >= 6.0)
        {
            hh = 0.0;
        }

        int i = (int)hh;
        double ff = hh - i;
        double p = v * (1.0 - s);
        double q = v * (1.0 - (s * ff));
        double t = v * (1.0 - (s * (1.0 - ff)));

        return i switch
        {
            0 => [v, t, p],
            1 => [q, v, p],
            2 => [p, v, t],
            3 => [p, q, v],
            4 => [t, p, v],
            _ => [v, p, q]
        };
    }
}