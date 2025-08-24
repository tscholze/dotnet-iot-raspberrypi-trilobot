# SignalR Hub Reference — TriloBot

This document lists the SignalR hub endpoint, callable methods, events, and the `Lights` enum mapping used by the hub.

Hub endpoint
- `/trilobotHub` — constant `TriloBot.Web.SignalR.TriloBotHub.HubEndpoint`

Events (Hub -> Client)
- `DistanceUpdated` (double): broadcast when the ultrasonic distance reading changed.
- `ObjectTooNearUpdated` (bool): broadcast when proximity threshold is crossed.

Callable methods (Client -> Hub)
The hub exposes the following methods. Names and parameter lists match the server-side `TriloBotHub` implementation.

- `SetButtonLed(int lightId, double value)`
  - Set brightness for a button LED. `lightId` is a numeric value from the `Lights` enum.

- `FillUnderlighting(byte r, byte g, byte b)`
  - Fill all underlighting LEDs with the given RGB color.

- `SetUnderlight(string light, byte r, byte g, byte b)`
  - Set a single underlight. The `light` string is parsed into the `Lights` enum, e.g. `"LIGHT_FRONT_RIGHT"`.

- `ClearUnderlighting()`
  - Turn off all underlighting LEDs.

- `StartPoliceEffect()`
  - Start the built-in police light effect.

- `Move(double horizontal, double vertical)`
  - Drive command. `horizontal` in [-1.0, 1.0] (left to right). `vertical` in [-1.0, 1.0] (back to forward).

- `Task<string> TakePhoto(string savePath)`
  - Take a photo and save to `savePath`. Returns saved file path as string.

- `StartDistanceMonitoring()`
  - Start server-side background monitoring of the ultrasonic sensor.

- `Task<double> ReadDistance()`
  - Read the current ultrasonic distance and return the value (cm).

Lifecycle
- `Close()` is implemented server-side to dispose the underlying `TriloBot` instance and subscriptions. This is intended for host lifecycle management and typically not invoked by normal clients.

Lights enum reference
The hub uses the `TriloBot.Light.Lights` enum. Below is the mapping of enum names to numeric IDs and localized names.

| ID | Enum name | Localized name | GPIO pin (button LEDs only) |
| --:|-----------|----------------|----------------------------:|
| 0  | LIGHT_FRONT_RIGHT | Front Right | - |
| 1  | LIGHT_FRONT_LEFT  | Front Left  | - |
| 2  | LIGHT_MIDDLE_LEFT | Middle Left | - |
| 3  | LIGHT_REAR_LEFT   | Rear Left   | - |
| 4  | LIGHT_REAR_RIGHT  | Rear Right  | - |
| 5  | LIGHT_MIDDLE_RIGHT| Middle Right| - |
| 6  | LIGHT_LED_A       | Button A LED| 23 |
| 7  | LIGHT_LED_B       | Button B LED| 22 |
| 8  | LIGHT_LED_X       | Button X LED| 17 |
| 9  | LIGHT_LED_Y       | Button Y LED| 27 |

Notes:
- Only the button LEDs (IDs 6–9) have GPIO pin mappings via `LightsExtensions.ToPinNumber()`.
- When calling `SetButtonLed` from clients, pass the numeric ID (e.g. `6`) or call via a small helper that maps enum names to IDs.

Examples

C# client (SignalR client):

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("https://robot-host/trilobotHub")
    .Build();

connection.On<double>("DistanceUpdated", d => Console.WriteLine($"Distance: {d} cm"));
connection.On<bool>("ObjectTooNearUpdated", near => Console.WriteLine($"Too near: {near}"));

await connection.StartAsync();
await connection.InvokeAsync("StartDistanceMonitoring");
await connection.InvokeAsync("Move", 0.0, 1.0);
await connection.InvokeAsync("FillUnderlighting", (byte)0, (byte)255, (byte)0);
await connection.InvokeAsync("SetButtonLed", 6, 0.9);
```

JavaScript client (browser):

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/trilobotHub')
  .build();

connection.on('DistanceUpdated', d => console.log('distance', d));
connection.on('ObjectTooNearUpdated', v => console.log('near', v));

await connection.start();
await connection.invoke('StartDistanceMonitoring');
await connection.invoke('Move', 0.0, 1.0);
await connection.invoke('FillUnderlighting', 0, 255, 0);
await connection.invoke('SetButtonLed', 6, 0.9);
```

Security and networking
- Ensure clients can reach the robot host by IP/hostname and open ports.
- For production add authentication/authorization to the hub.

--
Generated from the current `TriloBotHub` implementation and `Lights` enum.
