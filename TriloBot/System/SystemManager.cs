using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TriloBot.SystemInfo;

/// <summary>
/// Manages system information and monitoring for Raspberry Pi devices.
/// Provides access to system properties like hostname, IP addresses, CPU info,
/// and real-time monitoring of CPU and memory usage through observables.
/// </summary>
public class SystemManager : IDisposable
{
    #region Constants

    /// <summary>
    /// Default interval for system monitoring updates in milliseconds.
    /// </summary>
    private const int DefaultMonitoringInterval = 2000;

    /// <summary>
    /// Path to the CPU info file on Linux systems.
    /// </summary>
    private const string CpuInfoPath = "/proc/cpuinfo";

    /// <summary>
    /// Path to the memory info file on Linux systems.
    /// </summary>
    private const string MemInfoPath = "/proc/meminfo";

    /// <summary>
    /// Path to the system load average file on Linux systems.
    /// </summary>
    private const string LoadAvgPath = "/proc/loadavg";

    /// <summary>
    /// Path to the system uptime file on Linux systems.
    /// </summary>
    private const string UptimePath = "/proc/uptime";

    /// <summary>
    /// Path to the CPU temperature file on Raspberry Pi.
    /// </summary>
    private const string ThermalPath = "/sys/class/thermal/thermal_zone0/temp";

    #endregion

    #region Private Fields

    /// <summary>
    /// Cancellation token source for monitoring tasks.
    /// </summary>
    private CancellationTokenSource? _monitoringCts;

    /// <summary>
    /// Task for background system monitoring.
    /// </summary>
    private Task? _monitoringTask;

    /// <summary>
    /// Subject for CPU usage percentage updates.
    /// </summary>
    private readonly BehaviorSubject<double> _cpuUsageSubject = new(0.0);

    /// <summary>
    /// Subject for memory usage percentage updates.
    /// </summary>
    private readonly BehaviorSubject<double> _memoryUsageSubject = new(0.0);

    /// <summary>
    /// Subject for CPU temperature updates (in Celsius).
    /// </summary>
    private readonly BehaviorSubject<double> _cpuTemperatureSubject = new(0.0);

    /// <summary>
    /// Previous CPU stats for calculating usage percentage.
    /// </summary>
    private (long idle, long total) _previousCpuStats = (0, 0);

    /// <summary>
    /// Tracks whether the object has been disposed.
    /// </summary>
    private bool _disposed;

    #endregion

    #region Public Properties

    /// <summary>
    /// Observable stream of CPU usage percentage (0.0 to 100.0).
    /// </summary>
    public IObservable<double> CpuUsageObservable => _cpuUsageSubject.AsObservable();

    /// <summary>
    /// Observable stream of memory usage percentage (0.0 to 100.0).
    /// </summary>
    public IObservable<double> MemoryUsageObservable => _memoryUsageSubject.AsObservable();

    /// <summary>
    /// Observable stream of CPU temperature in Celsius.
    /// </summary>
    public IObservable<double> CpuTemperatureObservable => _cpuTemperatureSubject.AsObservable();

    /// <summary>
    /// Gets the system hostname.
    /// </summary>
    public string Hostname => Environment.MachineName;

    /// <summary>
    /// Gets the operating system description.
    /// </summary>
    public string OperatingSystem => RuntimeInformation.OSDescription;

    /// <summary>
    /// Gets the system architecture (e.g., ARM64, ARM32).
    /// </summary>
    public string Architecture => RuntimeInformation.OSArchitecture.ToString();

    /// <summary>
    /// Gets the .NET runtime version.
    /// </summary>
    public string RuntimeVersion => RuntimeInformation.FrameworkDescription;

    /// <summary>
    /// Gets the current user name.
    /// </summary>
    public string CurrentUser => Environment.UserName;

    /// <summary>
    /// Gets the system uptime as a TimeSpan.
    /// Returns TimeSpan.Zero if unable to read uptime.
    /// </summary>
    public TimeSpan SystemUptime
    {
        get
        {
            try
            {
                if (File.Exists(UptimePath))
                {
                    var uptimeText = File.ReadAllText(UptimePath).Split()[0];
                    if (double.TryParse(uptimeText, out var uptimeSeconds))
                    {
                        return TimeSpan.FromSeconds(uptimeSeconds);
                    }
                }
            }
            catch
            {
                // Fall back to process uptime if system uptime is unavailable
                return DateTime.Now - Process.GetCurrentProcess().StartTime;
            }
            
            return TimeSpan.Zero;
        }
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemManager"/> class.
    /// </summary>
    public SystemManager()
    {
        // Validate that we're running on a supported platform
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            throw new PlatformNotSupportedException("SystemManager is only supported on Linux platforms.");
        }
    }

    #endregion

    #region System Information Methods

    /// <summary>
    /// Gets all available network interface IP addresses.
    /// </summary>
    /// <returns>A dictionary mapping interface names to their IP addresses.</returns>
    public Dictionary<string, List<string>> GetNetworkInterfaces()
    {
        var interfaces = new Dictionary<string, List<string>>();

        try
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus == OperationalStatus.Up)
                {
                    var addresses = new List<string>();
                    
                    foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        // Skip loopback and link-local addresses
                        if (!IPAddress.IsLoopback(address.Address) && 
                            address.Address.AddressFamily == global::System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            addresses.Add(address.Address.ToString());
                        }
                    }

                    if (addresses.Count > 0)
                    {
                        interfaces[networkInterface.Name] = addresses;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading network interfaces: {ex.Message}");
        }

        return interfaces;
    }

    /// <summary>
    /// Gets the primary IP address of the system.
    /// </summary>
    /// <returns>The primary IP address as a string, or "Unknown" if not available.</returns>
    public string GetPrimaryIpAddress()
    {
        try
        {
            var interfaces = GetNetworkInterfaces();
            
            // Prefer wlan0 (Wi-Fi) or eth0 (Ethernet) interfaces
            foreach (var preferredInterface in new[] { "wlan0", "eth0" })
            {
                if (interfaces.TryGetValue(preferredInterface, out var addresses) && addresses.Count > 0)
                {
                    return addresses[0];
                }
            }

            // Fall back to any available interface
            foreach (var kvp in interfaces)
            {
                if (kvp.Value.Count > 0)
                {
                    return kvp.Value[0];
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting primary IP address: {ex.Message}");
        }

        return "Unknown";
    }

    /// <summary>
    /// Gets CPU information from /proc/cpuinfo.
    /// </summary>
    /// <returns>A dictionary containing CPU information.</returns>
    public Dictionary<string, string> GetCpuInfo()
    {
        var cpuInfo = new Dictionary<string, string>();

        try
        {
            if (File.Exists(CpuInfoPath))
            {
                var lines = File.ReadAllLines(CpuInfoPath);
                
                foreach (var line in lines)
                {
                    var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
                    if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                    {
                        // Use the first occurrence of each key (processor 0 info)
                        if (!cpuInfo.ContainsKey(parts[0]))
                        {
                            cpuInfo[parts[0]] = parts[1];
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading CPU info: {ex.Message}");
        }

        return cpuInfo;
    }

    /// <summary>
    /// Gets memory information from /proc/meminfo.
    /// </summary>
    /// <returns>A dictionary containing memory information in KB.</returns>
    public Dictionary<string, long> GetMemoryInfo()
    {
        var memInfo = new Dictionary<string, long>();

        try
        {
            if (File.Exists(MemInfoPath))
            {
                var lines = File.ReadAllLines(MemInfoPath);
                
                foreach (var line in lines)
                {
                    var match = Regex.Match(line, @"^(\w+):\s*(\d+)\s*kB?$");
                    if (match.Success)
                    {
                        var key = match.Groups[1].Value;
                        if (long.TryParse(match.Groups[2].Value, out var value))
                        {
                            memInfo[key] = value; // Value in KB
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading memory info: {ex.Message}");
        }

        return memInfo;
    }

    /// <summary>
    /// Gets the current CPU temperature in Celsius.
    /// </summary>
    /// <returns>CPU temperature in Celsius, or 0.0 if not available.</returns>
    public double GetCpuTemperature()
    {
        try
        {
            if (File.Exists(ThermalPath))
            {
                var tempText = File.ReadAllText(ThermalPath).Trim();
                if (int.TryParse(tempText, out var tempMilliCelsius))
                {
                    // Temperature is in millicelsius, convert to celsius
                    return tempMilliCelsius / 1000.0;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading CPU temperature: {ex.Message}");
        }

        return 0.0;
    }

    /// <summary>
    /// Gets the system load averages for 1, 5, and 15 minutes.
    /// </summary>
    /// <returns>A tuple containing (load1min, load5min, load15min).</returns>
    public (double load1min, double load5min, double load15min) GetLoadAverages()
    {
        try
        {
            if (File.Exists(LoadAvgPath))
            {
                var loadText = File.ReadAllText(LoadAvgPath).Trim();
                var parts = loadText.Split(' ');
                
                if (parts.Length >= 3 &&
                    double.TryParse(parts[0], out var load1) &&
                    double.TryParse(parts[1], out var load5) &&
                    double.TryParse(parts[2], out var load15))
                {
                    return (load1, load5, load15);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading load averages: {ex.Message}");
        }

        return (0.0, 0.0, 0.0);
    }

    #endregion

    #region Monitoring Methods

    /// <summary>
    /// Starts background monitoring of system resources.
    /// Updates CPU usage, memory usage, and CPU temperature observables.
    /// </summary>
    /// <param name="intervalMs">Monitoring interval in milliseconds. Defaults to 2000ms.</param>
    public void StartMonitoring(int intervalMs = DefaultMonitoringInterval)
    {
        if (_monitoringTask is { IsCompleted: false })
        {
            return; // Already running
        }

        _monitoringCts = new CancellationTokenSource();

        _monitoringTask = Task.Run(async () =>
        {
            while (!_monitoringCts.Token.IsCancellationRequested)
            {
                try
                {
                    // Update CPU usage
                    var cpuUsage = GetCurrentCpuUsage();
                    _cpuUsageSubject.OnNext(cpuUsage);

                    // Update memory usage
                    var memoryUsage = GetCurrentMemoryUsage();
                    _memoryUsageSubject.OnNext(memoryUsage);

                    // Update CPU temperature
                    var temperature = GetCpuTemperature();
                    _cpuTemperatureSubject.OnNext(temperature);

                    await Task.Delay(intervalMs, _monitoringCts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in system monitoring: {ex.Message}");
                    await Task.Delay(intervalMs, _monitoringCts.Token);
                }
            }
        }, _monitoringCts.Token);
    }

    /// <summary>
    /// Stops the background system monitoring.
    /// </summary>
    public void StopMonitoring()
    {
        if (_monitoringCts != null)
        {
            _monitoringCts.Cancel();

            try
            {
                _monitoringTask?.Wait(5000);
            }
            catch (AggregateException) { }
            catch (OperationCanceledException) { }
            finally
            {
                _monitoringCts.Dispose();
                _monitoringCts = null;
                _monitoringTask = null;
            }
        }
    }

    /// <summary>
    /// Gets the current CPU usage percentage by reading /proc/stat.
    /// </summary>
    /// <returns>CPU usage percentage (0.0 to 100.0).</returns>
    private double GetCurrentCpuUsage()
    {
        try
        {
            var statPath = "/proc/stat";
            if (!File.Exists(statPath))
            {
                return 0.0;
            }

            var lines = File.ReadAllLines(statPath);
            var cpuLine = lines[0]; // First line contains overall CPU stats
            
            // Parse CPU stats: user nice system idle iowait irq softirq steal guest guest_nice
            var parts = cpuLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 5 || parts[0] != "cpu")
            {
                return 0.0;
            }

            // Calculate total and idle time
            long totalTime = 0;
            long idleTime = 0;

            // Sum all time values (skip the "cpu" label)
            for (int i = 1; i < parts.Length; i++)
            {
                if (long.TryParse(parts[i], out var time))
                {
                    totalTime += time;
                    
                    // Idle time is the 4th field (index 4, but we start from index 1)
                    if (i == 4)
                    {
                        idleTime = time;
                    }
                }
            }

            // Calculate CPU usage since last reading
            if (_previousCpuStats.total > 0)
            {
                var totalDelta = totalTime - _previousCpuStats.total;
                var idleDelta = idleTime - _previousCpuStats.idle;
                
                if (totalDelta > 0)
                {
                    var usage = 100.0 * (1.0 - (double)idleDelta / totalDelta);
                    _previousCpuStats = (idleTime, totalTime);
                    return Math.Max(0.0, Math.Min(100.0, usage)); // Clamp to 0-100%
                }
            }

            _previousCpuStats = (idleTime, totalTime);
            return 0.0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating CPU usage: {ex.Message}");
            return 0.0;
        }
    }

    /// <summary>
    /// Gets the current memory usage percentage.
    /// </summary>
    /// <returns>Memory usage percentage (0.0 to 100.0).</returns>
    private double GetCurrentMemoryUsage()
    {
        try
        {
            var memInfo = GetMemoryInfo();
            
            if (memInfo.TryGetValue("MemTotal", out var total) &&
                memInfo.TryGetValue("MemAvailable", out var available))
            {
                if (total > 0)
                {
                    var used = total - available;
                    return Math.Max(0.0, Math.Min(100.0, (double)used / total * 100.0));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating memory usage: {ex.Message}");
        }

        return 0.0;
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes the SystemManager and stops all monitoring tasks.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopMonitoring();
        
        _cpuUsageSubject.Dispose();
        _memoryUsageSubject.Dispose();
        _cpuTemperatureSubject.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}
