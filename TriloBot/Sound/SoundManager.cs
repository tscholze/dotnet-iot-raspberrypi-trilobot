using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TriloBot.Sound;

/// <summary>
/// Manages sound operations for the TriloBot robot, providing audio playback capabilities
/// on Raspberry Pi using ALSA (Advanced Linux Sound Architecture).
/// </summary>
public class SoundManager : IDisposable 
{
    #region Private Fields

    /// <summary>
    /// Tracks whether the object has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// The default sound directory path for audio files.
    /// </summary>
    private readonly string _soundDirectory;

    /// <summary>
    /// The default volume level for audio playback (0.0 to 1.0).
    /// </summary>
    private double _defaultVolume = 0.8;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SoundManager"/> class.
    /// </summary>
    /// <param name="soundDirectory">The directory path where sound files are stored. If null, uses current directory.</param>
    /// <exception cref="PlatformNotSupportedException">Thrown when running on non-Linux platforms.</exception>
    public SoundManager(string soundDirectory = "~/Music/")
    {
        // Validate platform support - sound playback is only supported on Linux (Raspberry Pi)
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            throw new PlatformNotSupportedException("SoundManager is only supported on Linux platforms with ALSA.");
        }

        _soundDirectory = soundDirectory;

        // Ensure the sound directory exists
        if (!Directory.Exists(_soundDirectory))
        {
            Directory.CreateDirectory(_soundDirectory);
        }

        // Log successful initialization
        Console.WriteLine($"SoundManager initialized with sound directory: {_soundDirectory}");
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets or sets the default volume level for audio playback.
    /// </summary>
    /// <value>Volume level between 0.0 (mute) and 1.0 (maximum volume).</value>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is outside the range [0.0, 1.0].</exception>
    public double DefaultVolume
    {
        get => _defaultVolume;
        set
        {
            if (value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Volume must be between 0.0 and 1.0");
            }
            _defaultVolume = value;
        }
    }

    /// <summary>
    /// Gets the current sound directory path.
    /// </summary>
    public string SoundDirectory => _soundDirectory;

    #endregion

    #region Public Methods

    /// <summary>
    /// Plays the horn.wav sound file asynchronously using the default volume.
    /// This is a convenience method for the most common sound effect.
    /// </summary>
    /// <returns>A task representing the asynchronous playback operation.</returns>
    /// <exception cref="FileNotFoundException">Thrown when horn.wav file is not found in the sound directory.</exception>
    public async Task PlayHornAsync()
    {
        await PlaySoundAsync("horn.wav");
    }

    /// <summary>
    /// Plays the horn.wav sound file synchronously using the default volume.
    /// This method blocks until playback is complete.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown when horn.wav file is not found in the sound directory.</exception>
    public void PlayHorn()
    {
        PlaySound("horn.wav");
    }

    /// <summary>
    /// Plays a specified sound file asynchronously using the default volume.
    /// </summary>
    /// <param name="fileName">The name of the sound file to play (with extension).</param>
    /// <returns>A task representing the asynchronous playback operation.</returns>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified sound file is not found.</exception>
    public async Task PlaySoundAsync(string fileName)
    {
        await PlaySoundAsync(fileName, _defaultVolume);
    }

    /// <summary>
    /// Plays a specified sound file asynchronously with a custom volume level.
    /// </summary>
    /// <param name="fileName">The name of the sound file to play (with extension).</param>
    /// <param name="volume">Volume level between 0.0 (mute) and 1.0 (maximum volume).</param>
    /// <returns>A task representing the asynchronous playback operation.</returns>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is outside the range [0.0, 1.0].</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified sound file is not found.</exception>
    public async Task PlaySoundAsync(string fileName, double volume)
    {
        ValidatePlaybackParameters(fileName, volume);

        // Build the full path to the sound file
        var fullPath = Path.Combine(_soundDirectory, fileName);
        
        // Verify the file exists before attempting playback
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Sound file not found: {fullPath}");
        }

        try
        {
            // Execute the sound playback command asynchronously using aplay (ALSA player)
            await ExecuteSoundCommand(fullPath, volume);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing sound '{fileName}': {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Plays a specified sound file synchronously using the default volume.
    /// This method blocks until playback is complete.
    /// </summary>
    /// <param name="fileName">The name of the sound file to play (with extension).</param>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified sound file is not found.</exception>
    public void PlaySound(string fileName)
    {
        PlaySound(fileName, _defaultVolume);
    }

    /// <summary>
    /// Plays a specified sound file synchronously with a custom volume level.
    /// This method blocks until playback is complete.
    /// </summary>
    /// <param name="fileName">The name of the sound file to play (with extension).</param>
    /// <param name="volume">Volume level between 0.0 (mute) and 1.0 (maximum volume).</param>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is outside the range [0.0, 1.0].</exception>
    /// <exception cref="FileNotFoundException">Thrown when the specified sound file is not found.</exception>
    public void PlaySound(string fileName, double volume)
    {
        // Use the async version and wait for completion
        PlaySoundAsync(fileName, volume).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Checks if a specified sound file exists in the sound directory.
    /// </summary>
    /// <param name="fileName">The name of the sound file to check (with extension).</param>
    /// <returns>True if the file exists, false otherwise.</returns>
    /// <exception cref="ArgumentException">Thrown when fileName is null or empty.</exception>
    public bool SoundFileExists(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        var fullPath = Path.Combine(_soundDirectory, fileName);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Gets a list of all sound files in the sound directory.
    /// </summary>
    /// <returns>An array of sound file names (with extensions).</returns>
    public string[] GetAvailableSoundFiles()
    {
        // Common audio file extensions supported by ALSA
        var audioExtensions = new[] { "*.wav", "*.mp3", "*.ogg", "*.flac", "*.aac" };
        
        var soundFiles = new List<string>();
        
        // Search for files with supported audio extensions
        foreach (var extension in audioExtensions)
        {
            var files = Directory.GetFiles(_soundDirectory, extension, SearchOption.TopDirectoryOnly);
            soundFiles.AddRange(files.Select(Path.GetFileName).Where(name => !string.IsNullOrEmpty(name))!);
        }
        
        return soundFiles.ToArray();
    }

    /// <summary>
    /// Sets the system volume level (master volume) using ALSA mixer.
    /// This affects the overall system audio output.
    /// </summary>
    /// <param name="volume">Volume level between 0.0 (mute) and 1.0 (maximum volume).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is outside the range [0.0, 1.0].</exception>
    public async Task SetSystemVolumeAsync(double volume)
    {
        if (volume < 0.0 || volume > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");
        }

        try
        {
            // Convert volume percentage to ALSA percentage (0-100%)
            var alsaVolume = (int)(volume * 100);
            
            // Use amixer to set the master volume
            var processInfo = new ProcessStartInfo
            {
                FileName = "amixer",
                Arguments = $"set Master {alsaVolume}%",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting system volume: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Validates the parameters for sound playback operations.
    /// </summary>
    /// <param name="fileName">The file name to validate.</param>
    /// <param name="volume">The volume level to validate.</param>
    /// <exception cref="ArgumentException">Thrown when fileName is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when volume is invalid.</exception>
    private static void ValidatePlaybackParameters(string fileName, double volume)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        if (volume < 0.0 || volume > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0.0 and 1.0");
        }
    }

    /// <summary>
    /// Executes the sound playback command using ALSA's aplay utility.
    /// </summary>
    /// <param name="fullPath">The full path to the sound file.</param>
    /// <param name="volume">The volume level for playback.</param>
    /// <returns>A task representing the asynchronous command execution.</returns>
    private static async Task ExecuteSoundCommand(string fullPath, double volume)
    {
        // Create process info for aplay command with volume control
        var processInfo = new ProcessStartInfo
        {
            FileName = "aplay",
            Arguments = $"\"{fullPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Set volume using environment variable if needed
        // Note: Individual file volume control via aplay is limited
        // For precise volume control, we'd need to use additional tools like sox
        if (Math.Abs(volume - 1.0) > 0.01) // If volume is not at maximum
        {
            // Use amixer to temporarily adjust volume, then restore
            // This is a simplified approach - in production, you might want more sophisticated volume control
            processInfo.FileName = "sh";
            processInfo.Arguments = $"-c \"amixer set Master {(int)(volume * 100)}% > /dev/null 2>&1 && aplay '{fullPath}'\"";
        }

        // Execute the command and wait for completion
        using var process = Process.Start(processInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
            
            // Check if the process completed successfully
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"Sound playback failed: {error}");
            }
        }
        else
        {
            throw new InvalidOperationException("Failed to start sound playback process.");
        }
    }

    #endregion

    #region IDisposable Support

    /// <summary>
    /// Disposes the SoundManager and cleans up any resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        
        Console.WriteLine("Disposing SoundManager...");
        
        // No specific cleanup required for sound operations
        // ALSA handles process cleanup automatically
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}