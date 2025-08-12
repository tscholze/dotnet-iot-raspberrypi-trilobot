# Copilot Instructions for TriloBot.NET

## Overview
TriloBot.NET is a C# .NET library designed to control the Pimoroni Trilobot robot platform on a Raspberry Pi. The project integrates .NET IoT, SignalR, Blazor, and .NET MAUI to provide a comprehensive API and user interface for managing Trilobot hardware components.

## Architecture
- **Core Components**:
  - `ButtonManager`: Handles button state, debouncing, and events.
  - `LightManager`: Controls all LEDs and underlighting.
  - `MotorManager`: Abstracts motor control and movement.
  - `UltrasoundManager`: Provides distance readings and proximity events.
  - `CameraManager`: Manages photo captures and video streaming.
- **Main Class**: `TriloBot` composes all manager classes and exposes high-level control methods.
- **UI**: Built using .NET MAUI and Blazor for cross-platform compatibility.
- **Communication**: SignalR is used for real-time communication between components.

## Developer Workflows
### Building the Project
1. Clone the repository:
   ```sh
   git clone https://github.com/tscholze/dotnet-iot-raspberrypi-trilobot.git
   cd dotnet-iot-raspberrypi-trilobot
   ```
2. Build the solution:
   ```sh
   dotnet build
   ```

### Running the Project
- **Demo Application**:
  ```sh
  dotnet run --project TriloBot
  ```
- **Web Client**:
  ```sh
  dotnet run --project TriloBot.Blazor
  ```
- **Webcam Feed**:
  ```sh
  cd _thirdparty/webrtc && mediamtx
  ```

### Testing
- Unit tests are located in the `Tests` directory (if applicable).
- Run tests using:
  ```sh
  dotnet test
  ```

## Project-Specific Conventions
- **Manager Classes**: Each hardware subsystem is encapsulated in a dedicated manager class for modularity and clarity.
- **Enums and Extensions**: Used extensively for hardware mappings to improve code readability and maintainability.
- **Observables**: Reactive programming patterns are used for event handling (e.g., button presses, proximity alerts).

## Integration Points
- **External Dependencies**:
  - `System.Device.Gpio`: For GPIO interactions.
  - `MediaMTX`: For video streaming (binary must be placed in `_thirdparty/webrtc`).
- **Cross-Component Communication**:
  - SignalR is used for real-time updates between the robot and UI components.

## Key Files and Directories
- `TriloBot/`: Core library containing manager classes and the main `TriloBot` class.
- `TriloBot.Maui/`: .NET MAUI app for cross-platform UI.
- `TriloBot.Blazor/`: Blazor app for web-based control.
- `_thirdparty/webrtc/`: Contains the MediaMTX binary for video streaming.

## Examples
### Basic Usage
```csharp
using var robot = new TriloBot();

// Start distance monitoring and react to proximity
robot.StartDistanceMonitoring();
robot.ObjectTooNearObservable.Subscribe(tooNear =>
{
    if (tooNear)
        robot.FillUnderlighting(255, 0, 0); // Red if too close
    else
        robot.FillUnderlighting(0, 255, 0); // Green otherwise
});

// Listen for button presses
robot.StartButtonMonitoring();
robot.ButtonPressedObservable.Subscribe(button =>
{
    if (button == Buttons.ButtonA)
        robot.Forward();
    else if (button == Buttons.ButtonB)
        robot.Backward();
    else if (button == Buttons.ButtonX)
        robot.TurnLeft();
    else if (button == Buttons.ButtonY)
        robot.TurnRight();
});

// Set underlighting to blue
robot.FillUnderlighting(0, 0, 255);

// Take a photo (async)
string photoPath = await robot.TakePhotoAsync("/home/pi/photos");
Console.WriteLine($"Photo saved to: {photoPath}");
```

## Notes
- Ensure all GPIO, CSI, SPI, and IC2 interfaces are enabled on the Raspberry Pi.
- Use .NET 9.0 SDK or newer for compatibility.
- Contributions are welcome! Submit a Pull Request to improve the project.
