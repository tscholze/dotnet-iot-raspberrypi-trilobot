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
    /// Returns the SignalR hub endpoint for video streaming.
    /// The actual video stream should be handled by a SignalR hub in the web project.
    /// </summary>
    /// <returns>The SignalR hub URL for video streaming.</returns>
    public string GetLiveStreamSignalRHubUrl()
    {
        // This should match the SignalR hub route in your Blazor server/web project
        return "/cameraStreamHub";
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

    /// <summary>
    /// Starts the live video stream and returns the URL for the video element.
    /// </summary>
    /// <returns>The URL for the live video stream.</returns>
    public string StartLiveStream()
    {
        // Assuming the live stream is hosted on a specific endpoint
        return "http://localhost:5000/live-stream"; // Replace with the actual stream URL
    }

    /// <summary>
    /// Stops the live video stream.
    /// </summary>
    public void StopLiveStream()
    {
        // Logic to stop the live stream can be implemented here if needed
    }
}
