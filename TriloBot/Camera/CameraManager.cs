using System.Diagnostics;

namespace TriloBot.Camera;

/// <summary>
/// Manages camera operations for the TriloBot, including photo capture and video streaming.
/// </summary>
public class CameraManager
{
    /// <summary>
    /// Takes a photo using the system camera and saves it to the specified path.
    /// </summary>
    /// <param name="savePath">The directory to save the photo in.</param>
    /// <returns>The full file path of the saved photo.</returns>
    public async Task<string> TakePhotoAsync(string savePath)
    {
        Directory.CreateDirectory(savePath);
        var fileName = Path.Combine(savePath, $"photo_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "libcamera-still", // or "raspistill" depending on your Pi OS
                Arguments = $"-o {fileName} --nopreview -t 1000",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        await process.WaitForExitAsync();
        return fileName;
    }

    /// <summary>
    /// Starts recording a video using the system camera and saves it to the specified path.
    /// </summary>
    /// <param name="savePath">The directory to save the video in.</param>
    /// <param name="duration">The duration of the video in seconds.</param>
    /// <returns>The full file path of the saved video.</returns>
    public async Task<string> RecordVideoAsync(string savePath, int duration)
    {
        Directory.CreateDirectory(savePath);
        var fileName = Path.Combine(savePath, $"video_{DateTime.Now:yyyyMMdd_HHmmss}.h264");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "libcamera-vid", // or "raspivid" depending on your Pi OS
                Arguments = $"-o {fileName} --nopreview -t {duration * 1000}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        await process.WaitForExitAsync();
        return fileName;
    }
}
