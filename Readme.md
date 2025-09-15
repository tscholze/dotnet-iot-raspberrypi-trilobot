# 🤖 Trilobot.NET
<p align="center">
  <img src="_docs/image.png" height="200" alt="Image of the project" />
</p>

> A C# .NET library for controlling the [Pimoroni Trilobot](https://shop.pimoroni.com/products/trilobot) robot platform on a Raspberry Pi using .NET IoT. This project aims to provide a SignalR C# API for all TriloBot features. With a Blazor and a .NET MAUI app.

**Unleash the Full Power of the .NET Ecosystem with One Robot!** 🚀

TriloBot.NET is more than just a robot controller library - it's a **comprehensive demonstration of what the modern .NET ecosystem can achieve**. From hardcore low-level sensor interactions using .NET IoT, to real-time web applications with Blazor and SignalR, to native mobile apps with .NET MAUI - **all from the same unified codebase**.

This project showcases how .NET spans the entire technology stack:
- 🔌 **Hardware Integration**: Direct GPIO, I2C, SPI, and camera control on Raspberry Pi
- 🌐 **Web Technologies**: Real-time Blazor Server apps with SignalR for live telemetry
- 📱 **Mobile Development**: Native iOS and Android apps using .NET MAUI
- 🎮 **System Programming**: Low-level Linux input event processing for Xbox controllers
- 📊 **Real-time Monitoring**: System telemetry with reactive programming patterns
- 🏗️ **Clean Architecture**: Modern C# with dependency injection, observables, and SOLID principles

**One Language. One Platform. Infinite Possibilities.**

## 📋 Table of Contents

- [🤖 Trilobot.NET](#-trilobotnet)
  - [📋 Table of Contents](#-table-of-contents)
  - [🚀 What Does It Do?](#-what-does-it-do)
  - [Status](#status)
  - [How it looks](#how-it-looks)
    - [Outside](#outside)
    - [Android (Surface Duo)](#android-surface-duo)
    - [Windows](#windows)
  - [🔧 Hardware Components (Pimoroni Trilobot)](#-hardware-components-pimoroni-trilobot)
  - [🛠️ Architecture \& Core Components](#️-architecture--core-components)
    - [🕹️ ButtonManager](#️-buttonmanager)
    - [💡 LightManager](#-lightmanager)
    - [🦾 MotorManager](#-motormanager)
    - [📏 UltrasoundManager](#-ultrasoundmanager)
    - [📸 CameraManager](#-cameramanager)
    - [🖥️ SystemManager](#️-systemmanager)
    - [🎮 RemoteControllerManager](#-remotecontrollermanager)
  - [🏗️ Unified Architecture](#️-unified-architecture)
  - [🎮 Xbox Controller Support (wired Xbox 360)](#-xbox-controller-support-wired-xbox-360)
  - [� System Monitoring](#-system-monitoring)
  - [�🕸️ SignalR Hub API](#️-signalr-hub-api)
  - [🚀 Getting Started](#-getting-started)
    - [Prerequisites](#prerequisites)
    - [Installation](#installation)
  - [📖 Documentation \& Usage Examples](#-documentation--usage-examples)
    - [🚀 Complete Integration Example](#-complete-integration-example)
  - [🙏 Acknowledgments](#-acknowledgments)
  - [📜 License](#-license)
  - [🤝 Contributing](#-contributing)
  - [❤️ More IoT projects of mine](#️-more-iot-projects-of-mine)
    - [.NET on Raspberry Pi](#net-on-raspberry-pi)
    - [Windows 10 IoT Core apps](#windows-10-iot-core-apps)
    - [Android Things apps](#android-things-apps)
    - [Python scripts](#python-scripts)

## 🚀 What Does It Do?

This library provides easy-to-use manager classes for all major Trilobot hardware components:

- 🦾 **Driving around** – Drive, steer, and control both motors with speed and direction
- 🕹️ **Buttons** – Read and react to button presses (A, B, X, Y) with observable events
- 💡 **Lights, LEDs and more** – Control underlighting (RGB LEDs) and button LEDs, including color effects
- 📏 **Keep distance** – Measure distance and proximity with the ultrasonic sensor, with observable events
- 📸 **Live Feed** – Take photos and (optionally) stream live video (SignalR/MJPEG integration)
- 🎮 **Xbox Controller (wired 360)** – Remote-drive the robot: left stick steers, RT forward, LT backward; A/B/X/Y mapped to actions

## Status

| Service | Name                  | State                                                                                                                                                                                                                             |
| ------- | --------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Action  | Build TriloBot.Core   | [![.NET](https://github.com/tscholze/dotnet-iot-raspberrypi-trilobot/actions/workflows/dotnet-build-core.yml/badge.svg)](https://github.com/tscholze/dotnet-iot-raspberrypi-trilobot/actions/workflows/dotnet-build-core.yml)     |
| Action  | Build TriloBot.Web | [![.NET](https://github.com/tscholze/dotnet-iot-raspberrypi-trilobot/actions/workflows/dotnet-build-web.yml/badge.svg)](https://github.com/tscholze/dotnet-iot-raspberrypi-trilobot/actions/workflows/dotnet-build-web.yml) |
| Action  | Build TriloBot.Maui   | -                                                                                                                                                                                                                                 |

## How it looks

### Outside
|                                    |                                    |
| ---------------------------------- | ---------------------------------- |
| ![](_docs/trilobot-outside-1.jpeg) | ![](_docs/trilobot-outside-2.jpeg) |

### Android (Surface Duo)

|                                         |
| --------------------------------------- |
| ![](_docs/trilobot-android-surface.png) |
| ![](_docs/trilobot-maui-joystick.png)   |

### Windows
|                                  |                            |
| -------------------------------- | -------------------------- |
| ![](_docs/trilobot-web-edge.png) | ![](_docs/trilobot-vs.png) |
| ![](_docs/trilobot-windows.png)  |                            |


## 🔧 Hardware Components (Pimoroni Trilobot)

- 4 x Programmable Buttons (A, B, X, Y)
- 4 x Button LEDs (RGB)
- 6 x Underlighting RGB LEDs
- 2 x Motors (left/right, PWM control)
- 1 x Ultrasonic Distance Sensor
- 1 x Camera (Raspberry Pi Camera Module, optional)


## 🛠️ Architecture & Core Components

TriloBot.NET is built around specialized manager classes that handle each hardware subsystem. This modular approach demonstrates clean architecture principles and makes the codebase maintainable and testable.

### 🕹️ ButtonManager
**Physical Button Control & Event Processing**

Manages the four programmable buttons (A, B, X, Y) on the TriloBot with sophisticated debouncing and event handling.

**Key Features:**
- Hardware debouncing to prevent false triggers
- Reactive programming with observables for button press events
- Thread-safe button state management
- Edge detection (press/release events)

**Usage Example:**
```csharp
robot.StartButtonMonitoring();
robot.ButtonPressedObservable.Subscribe(button =>
{
    Console.WriteLine($"Button {button} pressed!");
    // React to specific buttons
    switch (button)
    {
        case Buttons.ButtonA: robot.Forward(); break;
        case Buttons.ButtonB: robot.Backward(); break;
        case Buttons.ButtonX: robot.TurnLeft(); break;
        case Buttons.ButtonY: robot.TurnRight(); break;
    }
});
```

---

### 💡 LightManager
**RGB LED Control & Visual Effects**

Controls all lighting systems including button LEDs and underlighting with support for color effects and animations.

**Key Features:**
- Individual button LED brightness control
- 6 RGB underlighting LEDs with full color spectrum
- Pre-built effects (police lights, color cycling)
- Hardware abstraction for different LED types (SN3218 chip)

**Usage Example:**
```csharp
// Set button LED brightness
robot.SetButtonLed(Lights.ButtonA, 0.8);

// Fill all underlights with color
robot.FillUnderlighting(255, 0, 128); // Pink

// Start animated effects
robot.StartPoliceEffect();

// Individual underlight control
robot.SetUnderlight(Lights.Light1, 0, 255, 0); // Green
```

---

### 🦾 MotorManager  
**Precision Motor Control & Movement**

Handles dual-motor control for robot movement with PWM speed control and directional logic.

**Key Features:**
- Independent left/right motor control
- PWM-based speed regulation (0-100%)
- High-level movement abstractions (forward, backward, turn)
- Smooth acceleration/deceleration curves
- Motor safety and overcurrent protection

**Usage Example:**
```csharp
// High-level movement
robot.Forward();
robot.Backward();
robot.TurnLeft();
robot.TurnRight();

// Precise control with speed
robot.Move(horizontal: 0.3, vertical: 0.8); // Slight right, fast forward

// Direct motor control
robot.SetMotorSpeed(MotorSide.Left, 75);   // 75% speed
robot.SetMotorSpeed(MotorSide.Right, 60);  // 60% speed
```

---

### 📏 UltrasoundManager
**Distance Sensing & Proximity Detection**

Manages the ultrasonic distance sensor with real-time monitoring and proximity alerting.

**Key Features:**
- Accurate distance measurements in centimeters
- Configurable proximity thresholds
- Real-time monitoring with observables
- Noise filtering and measurement averaging
- Object detection events

**Usage Example:**
```csharp
// Start distance monitoring
robot.StartDistanceMonitoring();

// React to distance changes
robot.DistanceObservable.Subscribe(distance =>
{
    Console.WriteLine($"Distance: {distance:F1} cm");
});

// Proximity alerts
robot.ObjectTooNearObservable.Subscribe(tooNear =>
{
    if (tooNear)
    {
        robot.FillUnderlighting(255, 0, 0); // Red warning
        robot.Stop(); // Emergency stop
    }
    else
    {
        robot.FillUnderlighting(0, 255, 0); // Green all-clear
    }
});

// Manual distance reading
double currentDistance = await robot.ReadDistanceAsync();
```

---

### 📸 CameraManager
**Image Capture & Video Processing**

Handles Raspberry Pi camera operations for photo capture and video streaming integration.

**Key Features:**
- High-quality photo capture with customizable settings
- Async/await support for non-blocking operations
- Integration with MediaMTX for live video streaming
- Configurable image formats and quality
- File system management for captured images

**Usage Example:**
```csharp
// Take a photo
string photoPath = await robot.TakePhotoAsync("/home/pi/photos");
Console.WriteLine($"Photo saved to: {photoPath}");

// Capture with custom settings
var settings = new CameraSettings 
{ 
    Width = 1920, 
    Height = 1080, 
    Quality = 95 
};
string hqPhoto = await robot.TakePhotoAsync("/tmp", settings);
```

---

### 🖥️ SystemManager
**System Monitoring & Telemetry**

Provides comprehensive Raspberry Pi system monitoring with real-time performance metrics.

**Key Features:**
- CPU usage monitoring with real-time updates
- Memory usage tracking (total, available, used)
- Temperature monitoring (CPU thermal sensor)
- Network information (hostname, IP addresses)
- System uptime and load averages
- Reactive observables for real-time telemetry

**Usage Example:**
```csharp
// Static system information
Console.WriteLine($"Hostname: {robot.GetHostname()}");
Console.WriteLine($"Primary IP: {robot.GetPrimaryIpAddress()}");
Console.WriteLine($"Total Memory: {robot.GetTotalMemoryMb()} MB");

// Start real-time monitoring
robot.StartSystemMonitoring();

// Subscribe to live updates
robot.CpuUsageObservable.Subscribe(cpu => 
    Console.WriteLine($"CPU: {cpu:F1}%"));
    
robot.MemoryUsageObservable.Subscribe(memory => 
    Console.WriteLine($"Memory: {memory:F1}%"));
    
robot.CpuTemperatureObservable.Subscribe(temp => 
    Console.WriteLine($"Temperature: {temp:F1}°C"));
```

---

### 🎮 RemoteControllerManager
**Xbox Controller Integration**

Advanced Xbox 360/Series controller support with low-level Linux input event processing.

**Key Features:**
- Direct Linux input subsystem integration (`/dev/input/event*`)
- Support for Xbox 360 (USB) and Xbox Series (Bluetooth) controllers
- Strategy pattern for different controller types
- Dead zone processing and input filtering
- Reactive observables for movement and button events
- Hardware auto-detection and connection management

**Usage Example:**
```csharp
// Controller setup with auto-detection
var controller = new RemoteControllerManager(ControllerType.Xbox360);

// Movement control
controller.HorizontalMovementObservable.Subscribe(horizontal =>
{
    // Left stick X controls steering (-1.0 to 1.0)
    robot.Steer(horizontal);
});

controller.VerticalMovementObservable.Subscribe(vertical =>
{
    // Triggers control forward/backward (RT - LT)
    robot.Drive(vertical);
});

// Button mapping
controller.ButtonPressedObservable.Subscribe(button =>
{
    switch (button)
    {
        case Buttons.ButtonA: robot.StartPoliceEffect(); break;
        case Buttons.ButtonB: robot.ClearUnderlighting(); break;
        case Buttons.ButtonX: robot.TakePhotoAsync("/tmp"); break;
        case Buttons.ButtonY: robot.Stop(); break;
    }
});

// Check connection status
if (controller.IsControllerConnected)
    Console.WriteLine("Controller ready!");
```

---

---

## 🏗️ Unified Architecture

All managers are composed in the main `TriloBot` class, which provides a unified API and coordinates between subsystems. The architecture demonstrates:

- **🎯 Single Responsibility Principle**: Each manager handles one hardware subsystem
- **🔄 Reactive Programming**: Extensive use of observables for event-driven architecture  
- **🏷️ Type Safety**: Enums and extension methods for hardware mappings
- **🧪 Testability**: Clean interfaces and dependency injection
- **📦 Modularity**: Components can be used independently or together
- **⚡ Performance**: Efficient resource management with proper disposal patterns

**Example of the Unified API:**
```csharp
using var robot = new TriloBot();

// All managers work together seamlessly
robot.StartButtonMonitoring();
robot.StartDistanceMonitoring();  
robot.StartSystemMonitoring();

// Coordinated responses across multiple subsystems
robot.ButtonPressedObservable.Subscribe(button =>
{
    if (button == Buttons.ButtonA)
    {
        robot.FillUnderlighting(0, 255, 0);        // Light manager
        robot.Forward();                           // Motor manager
        robot.TakePhotoAsync("/tmp");              // Camera manager
    }
});

// System-wide proximity safety
robot.ObjectTooNearObservable.Subscribe(tooNear =>
{
    if (tooNear)
    {
        robot.Stop();                              // Motor manager
        robot.FillUnderlighting(255, 0, 0);        // Light manager  
        robot.SetButtonLed(Lights.ButtonA, 1.0);  // Light manager
    }
});
```

## 🎮 Xbox Controller Support (wired Xbox 360)

TriloBot.NET can be remote-controlled using a wired Xbox 360 controller on Linux (Raspberry Pi OS). The `RemoteControllerManager` reads raw Linux input events from `/dev/input/event*` and exposes clean observables you can react to.

What’s supported:
- Left stick X controls horizontal steering (−1.0 … 1.0)
- Right Trigger (RT) drives forward (0.0 … 1.0)
- Left Trigger (LT) drives backward (0.0 … 1.0)
- A/B/X/Y buttons fire events (edge-triggered on press)

How to use:

```csharp
using var robot = new TriloBot();
using var manager = new TriloBot.RemoteController.RemoteControllerManager();

// Horizontal: map left stick X to left/right turning
controller.HorizontalMovementObservable.Subscribe(value =>
{
    roboter.move(...)
});

// Vertical: RT forward minus LT backward
controller.VerticalMovementObservable.Subscribe(value =>
{
    roboter.move(...)
});

// Buttons: map A/B/X/Y to actions
controller.ButtonPressedObservable.Subscribe(button =>
{
    switch(button) 
    {
        case A: ...
    }
});
```

Notes and requirements:
- Linux only (uses the input subsystem at `/dev/input/event*`).
- You may need permissions; if you get a permission error, run with `sudo` or add a udev rule to grant access.
- Dead zones and a movement threshold are applied to avoid noise and stick drift.

## � System Monitoring

TriloBot.NET includes comprehensive system monitoring capabilities via the `SystemManager` class:

**Static System Information:**
- Hostname, IP addresses, network interfaces
- CPU information (model, architecture, cores)
- Memory information (total, available, used)
- System uptime and load averages
- Operating system details

**Real-time Monitoring (via Observables):**
- CPU usage percentage (updated every 2 seconds by default)
- Memory usage percentage
- CPU temperature (Raspberry Pi thermal sensor)

Usage example:

```csharp
using var robot = new TriloBot();

// Get static information
Console.WriteLine($"Hostname: {robot.GetHostname()}");
Console.WriteLine($"Primary IP: {robot.GetPrimaryIpAddress()}");
Console.WriteLine($"CPU Temperature: {robot.GetCpuTemperature():F1}°C");

// Start real-time monitoring
robot.StartSystemMonitoring();

// Subscribe to updates
robot.CpuUsageObservable.Subscribe(cpu => 
    Console.WriteLine($"CPU: {cpu:F1}%"));
robot.MemoryUsageObservable.Subscribe(mem => 
    Console.WriteLine($"Memory: {mem:F1}%"));
robot.CpuTemperatureObservable.Subscribe(temp => 
    Console.WriteLine($"Temperature: {temp:F1}°C"));
```

## �🕸️ SignalR Hub API

TriloBot hosts a SignalR hub to expose robot commands and stream telemetry to connected clients.

Full reference and examples: `_docs/signalr.md`

Hub endpoint
- URL: `/trilobotHub` (see `TriloBotHub.HubEndpoint`)

Events (Hub -> Client)
- `DistanceUpdated` (double) — broadcast when the ultrasonic distance reading changes.
- `ObjectTooNearUpdated` (bool) — broadcast when proximity threshold is crossed.

Callable methods (Client -> Hub)
All method names below are available on the hub and match the server-side signatures:

- SetButtonLed(int lightId, double value)
    - Sets brightness of a button LED. `lightId` maps to `TriloBot.Light.Lights` enum values.
    - Example: `SetButtonLed(6, 0.75)`

- FillUnderlighting(byte r, byte g, byte b)
    - Fill all underlighting LEDs with the supplied RGB color.
    - Example: `FillUnderlighting(255, 0, 0)` (red)

- SetUnderlight(string light, byte r, byte g, byte b)
    - Set a single underlight by name (parsed to `Lights` enum).
    - Example: `SetUnderlight("Light1", 0, 128, 255)`

- ClearUnderlighting()
    - Turn off all underlighting.

- StartPoliceEffect()
    - Start a prebuilt light effect on the underlighting LEDs.

- Move(double horizontal, double vertical)
    - Move the robot. `horizontal` and `vertical` are normalized in `[-1.0, 1.0]`.
    - `horizontal`: left (-1) to right (+1), `vertical`: backward (-1) to forward (+1).
    - Example: `Move(0.2, 1.0)` — slight right while moving forward.

- Task<string> TakePhoto(string savePath)
    - Take a photo and save it to `savePath`. Returns the saved file path.

- StartDistanceMonitoring()
    - Start background distance monitoring on the robot (server-side polling).

- Task<double> ReadDistance()
    - Synchronously read the current distance and return the value (centimeters).

Notes on lifecycle methods
- The hub also exposes a server-side `Close()` method used to dispose subscriptions and the underlying `TriloBot` instance — this is intended for host lifecycle management and typically not invoked by normal clients.

Quick usage examples

C# (Microsoft.AspNetCore.SignalR.Client)

```csharp
var connection = new HubConnectionBuilder()
        .WithUrl("https://<robot-host>/trilobotHub")
        .Build();

connection.On<double>("DistanceUpdated", d => Console.WriteLine($"Distance: {d} cm"));
connection.On<bool>("ObjectTooNearUpdated", near => Console.WriteLine($"Too near: {near}"));

await connection.StartAsync();
await connection.InvokeAsync("StartDistanceMonitoring");
await connection.InvokeAsync("Move", 0.0, 1.0);
```

JavaScript (browser)

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl('/trilobotHub')
    .build();

connection.on('DistanceUpdated', d => console.log('distance', d));
connection.on('ObjectTooNearUpdated', v => console.log('near', v));

await connection.start();
await connection.invoke('StartDistanceMonitoring');
await connection.invoke('Move', 0.0, 1.0);
```

Security and networking
- Make sure the robot host is reachable from the client (IP/hostname, firewall, TLS when needed).
- Consider authentication/authorization for production deployments — the sample hub is intentionally simple for demos.

Video streaming
- Live video is provided separately via the MediaMTX pipeline; SignalR is only used for control and telemetry.

## 🚀 Getting Started

### Prerequisites
- Raspberry Pi 4
- Pimoroni Trilobot
- Enabled GPIO, CSI, SPI, IC2 interfaces
- .NET 9.0 SDK or newer
- Basic knowledge of C# and .NET
- Binary of [MediaMTX](https://github.com/bluenviron/mediamtx) must be placed into `_thirdparty/webrtc`

### Installation
1. Clone this repository:
   ```sh
   git clone https://github.com/tscholze/dotnet-iot-raspberrypi-trilobot.git
   cd dotnet-iot-raspberrypi-trilobot
   ```
2. Run the demo (see `TriloBot/Program.cs`):
   ```sh
   dotnet run --project TriloBot
   ```
3. To run for example the web client:
    ```sh
    dotnet run --project TriloBot.Web
    ```
4. To start the web cam feed:
   ```sh
   cd _thirdparty/webrtc && mediamtx
   ```

## 📖 Documentation & Usage Examples

Each manager class is fully documented with XML comments. See the source code for API details.

### 🚀 Complete Integration Example

This example showcases how multiple managers work together to create intelligent robot behavior:

```csharp
using var robot = new TriloBot();

// Initialize all systems
robot.StartButtonMonitoring();
robot.StartDistanceMonitoring();
robot.StartSystemMonitoring();

// Multi-system proximity safety
robot.ObjectTooNearObservable.Subscribe(tooNear =>
{
    if (tooNear)
    {
        robot.Stop();                              // Motor: Emergency stop
        robot.FillUnderlighting(255, 0, 0);        // Lights: Red alert
        Console.WriteLine("⚠️ Obstacle detected!"); 
    }
    else
    {
        robot.FillUnderlighting(0, 255, 0);        // Lights: Green all-clear
        Console.WriteLine("✅ Path clear");
    }
});

// Interactive button control
robot.ButtonPressedObservable.Subscribe(async button =>
{
    switch (button)
    {
        case Buttons.ButtonA:
            robot.Forward();
            robot.SetButtonLed(Lights.ButtonA, 1.0);
            break;
            
        case Buttons.ButtonB:
            robot.Backward();  
            robot.SetButtonLed(Lights.ButtonB, 1.0);
            break;
            
        case Buttons.ButtonX:
            robot.TurnLeft();
            robot.StartPoliceEffect(); // Visual feedback
            break;
            
        case Buttons.ButtonY:
            var photoPath = await robot.TakePhotoAsync("/tmp");
            robot.FillUnderlighting(255, 255, 0); // Yellow camera flash
            Console.WriteLine($"📸 Photo saved: {photoPath}");
            break;
    }
});

// System performance monitoring
robot.CpuUsageObservable.Subscribe(cpu => 
{
    // Visual CPU load indicator using button LEDs
    robot.SetButtonLed(Lights.ButtonA, cpu / 100.0);
});

robot.CpuTemperatureObservable.Subscribe(temp => 
{
    if (temp > 70) // Temperature warning
    {
        robot.FillUnderlighting(255, 165, 0); // Orange warning
        Console.WriteLine($"🌡️ High temperature: {temp:F1}°C");
    }
});

// Keep the program running
Console.WriteLine("🤖 TriloBot.NET is ready! Press any key to exit...");
Console.ReadKey();
```

This example demonstrates:
- **🔄 Cross-system coordination**: Proximity detection affects motors and lights
- **🎮 Interactive control**: Buttons trigger complex multi-system responses  
- **📊 Real-time monitoring**: System metrics drive visual feedback
- **🧠 Intelligent behavior**: Autonomous safety responses
- **📸 Async operations**: Non-blocking photo capture
- **✨ Visual effects**: Dynamic lighting based on system state


## 🙏 Acknowledgments
- [Pimoroni](https://shop.pimoroni.com/products/trilobot) for the Trilobot hardware
- .NET IoT team for the System.Device.Gpio library
- Community contributors


## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## ❤️ More IoT projects of mine
I like to tinker around with Raspberry Pis, I created a couple of educational apps and scripts regarding the Pi and sensors - mostly from Pimoroni.

### .NET on Raspberry Pi 
- [dotnet-iot-raspberrypi-blinkt](https://github.com/tscholze/dotnet-iot-raspberrypi-blinkt) A C# .NET implementation for the Pimoroni Blinkt! LED board on a Raspberry Pi.
- [dotnet-iot-raspberrypi-enviro](https://github.com/tscholze/dotnet-iot-raspberrypi-enviro) A C# .NET implementation for the Pimoroini Enviro HAT with BMP, TCS and more sensors
- [dotnet-iot-raspberrypi-rainbow](https://github.com/tscholze/dotnet-iot-raspberrypi-rainbow) A C# .NET implementation for the Pimoroini Rainbow HAT with Lights, BMP, segment displays and more

### Windows 10 IoT Core apps
- [dotnet-iot-homebear-blinkt](https://github.com/tscholze/dotnet-iot-homebear-blinkt) Windows 10 IoT Core UWP app that works great with the Pimoroni Blinkt! LED Raspberry Pi HAT.
- [dotnet-iot-homebear-tilt](https://github.com/tscholze/dotnet-iot-homebear-tilt) Windows 10 IoT Core UWP app that works great with the Pimoroni Pan and Tilt HAT (PIC16F1503)
- [dotnet-iot-homebear-rainbow](https://github.com/tscholze/dotnet-iot-homebear-rainbow) Windows 10 IoT Core UWP app that works great with the Pimoroni RainbowHAT

### Android Things apps
- [java-android-things-firebase-pager](https://github.com/tscholze/java-android-things-firebase-pager) An Android Things app that displays a Firebase Cloud Messaging notification on a alphanumeric segment control (Rainbow HAT)
- [java-android-things-tobot](https://github.com/tscholze/java-android-things-tobot) An Android Things an Google Assistant app to controll a Pimoroni STS vehicle by web and voice

### Python scripts
- [python-enviro-gdocs-logger](https://github.com/tscholze/python-enviro-gdocs-logger) Logs values like room temperature and more to a Google Docs Sheet with graphs
- [python-enviro-excel-online-logger](https://github.com/tscholze/python-enviro-excel-online-logger) Logs values like room temperature and more to a M365 Excel Sheet with graphs
- [python-enviro-azure-logger](https://github.com/tscholze/python-enviro-azure-logger) Logs values like room temperature and more to an Azure IoT Hub instance
