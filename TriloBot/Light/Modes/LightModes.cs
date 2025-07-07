using System;
using System.Threading;
using TriloBot.Light;

namespace TriloBot.Light.Modes;

/// <summary>
/// Provides light effect modes as extensions for <see cref="LightManager"/>.
/// </summary>
public static class LightModesExtensions
{
    /// <summary>
    /// Runs a police lights effect: left side red, right side blue, with rotation. Cancelable.
    /// </summary>
    /// <param name="lightManager">The <see cref="LightManager"/> instance.</param>
    /// <param name="cancellationToken">A cancellation token to stop the effect.</param>
    public static void PoliceLightsEffect(this LightManager lightManager, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            for (int i = 0; i < 6 && !cancellationToken.IsCancellationRequested; i++)
            {
                // Clear previous state
                lightManager.ClearUnderlighting();

                // Set red lights (rotating left to right)
                int redPos = i;
                lightManager.SetUnderlight(redPos, 255, 0, 0, false);
                lightManager.SetUnderlight((redPos + 1) % 6, 128, 0, 0, false);

                // Set blue lights (rotating right to left)
                int bluePos = (12 - i) % 6;
                lightManager.SetUnderlight(bluePos, 0, 0, 255, false);
                lightManager.SetUnderlight((bluePos + 1) % 6, 0, 0, 128);  // Show on last update

                Thread.Sleep(100);  // Adjust speed of rotation

                if (readDistance != null)
                {
                    Console.WriteLine($"Distance {readDistance()} cm");
                }
            }
        }
        lightManager.ClearUnderlighting();
    }
}

