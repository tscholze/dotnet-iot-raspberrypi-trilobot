# 🤖 Trilobot.NET

> A C# .NET library for controlling the [Pimoroni Trilobot](https://shop.pimoroni.com/products/trilobot) robot platform on a Raspberry Pi using .NET IoT. This project aims to provide a SignalR C# API for all TriloBot features. With a Blazor and a .NET MAUI app.


<center>
   <img src="_docs/image.png" height="200" alt="Image of the project" />
</center>

## 🚀 What Does It Do?

This library provides easy-to-use manager classes for all major Trilobot hardware components:

- 🦾 **Driving around** – Drive, steer, and control both motors with speed and direction
- 🕹️ **Buttons** – Read and react to button presses (A, B, X, Y) with observable events
- 💡 **Lights, LEDs and more** – Control underlighting (RGB LEDs) and button LEDs, including color effects
- 📏 **Keep distance** – Measure distance and proximity with the ultrasonic sensor, with observable events
- 📸 **Live Feed** – Take photos and (optionally) stream live video (SignalR/MJPEG integration)

## How it looks

### Outside
|                                    |                                    |
| ---------------------------------- | ---------------------------------- |
| ![](_docs/trilobot-outside-1.jpeg) | ![](_docs/trilobot-outside-2.jpeg) |

### Android (Surface Duo)

|                                         |     |
| --------------------------------------- | --- |
| ![](_docs/trilobot-android-surface.png) |     |

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


## 🛠️ Architecture

Each hardware subsystem is managed by its own class:

| Manager Class       | Responsibility                                       |
| ------------------- | ---------------------------------------------------- |
| `ButtonManager`     | Handles button state, debouncing, and events         |
| `LightManager`      | Controls all LEDs and underlighting                  |
| `MotorManager`      | Abstracts motor control and movement                 |
| `UltrasoundManager` | Provides distance readings and proximity events      |
| `CameraManager`     | Photo captures and other image related operations in |

All managers are composed in the main `TriloBot` class, which exposes observables and high-level control methods. All hardware mappings use enums and extension methods for clarity and maintainability.

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
    dotnet run --project TriloBot.Blazor
    ```
4. To start the web cam feed:
   ```sh
   cd _thirdparty/webrtc && mediamtx
   ```

## 📖 Documentation & Usage Examples

Each manager class is fully documented with XML comments. See the source code for API details.

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