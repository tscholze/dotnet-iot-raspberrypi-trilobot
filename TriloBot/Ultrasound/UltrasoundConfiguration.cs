namespace TriloBot.Ultrasound;

internal static class UltrasoundConfiguration
{
    internal const int UltraTrigPin = 13;
    internal const int UltraEchoPin = 25;

    internal const double SpeedOfSoundCmNs = 343 * 100.0 / 1E9; // 0.0000343 cm/ns

    internal const int Timeout = 50;
    internal const int Samples = 3;
}
