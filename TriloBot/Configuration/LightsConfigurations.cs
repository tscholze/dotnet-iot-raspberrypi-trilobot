using System;

namespace TriloBot.Configuration;

internal static class LightsConfigurations
{
    // Constants for lights
    internal const int LightFrontRight = 0;
    internal const int LightFrontLeft = 1;
    internal const int LightMiddleLeft = 2;
    internal const int LightRearLeft = 3;
    internal const int LightRearRight = 4;
    internal const int LightMiddleRight = 5;
    internal const int NumberOfLights = 6;
    

        internal static readonly int[] LightsLeft = { LightFrontLeft, LightMiddleLeft, LightRearLeft };
    internal static readonly int[] LightsRight = { LightFrontRight, LightMiddleRight, LightRearRight };
    internal static readonly int[] LightsFront = { LightFrontLeft, LightFrontRight };
    internal static readonly int[] LightsMiddle = { LightMiddleLeft, LightMiddleRight };
    internal static readonly int[] LightsRear = { LightRearLeft, LightRearRight };
    internal static readonly int[] LightsLeftDiagonal = { LightFrontLeft, LightRearRight };
    internal static readonly int[] LightsRightDiagonal = { LightFrontRight, LightRearLeft };
}
