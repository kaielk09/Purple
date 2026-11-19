# Purple — Game Telemetry Connector

Purple connects to telemetry explicitly published by **your own game** through a named Windows shared-memory channel. It does not inspect arbitrary process memory.

## Telemetry supported
- Game label and version
- Map/scene name
- Match state
- FPS and ping
- Score / round
- Objective status
- Event message

Player/enemy positions and entity lists are intentionally not part of this telemetry protocol.

## Build
Install the .NET 8 SDK on Windows, then run:

```powershell
dotnet run --project PurpleConnector.csproj
```

## Connect
Enter the telemetry channel's game key (not an EXE filename) and click **Connect**. Purple reads the JSON snapshot from the shared-memory channel named `PurpleTelemetry_<key>`.

The channel is created and populated by a telemetry publisher inside your game. Purple shows **CONNECTED** only after it receives and validates a snapshot.

## Protocol
The shared-memory block is UTF-8 JSON (up to 4096 bytes), with a null terminator. Example:

```json
{
  "game": "MyGame",
  "version": "0.1",
  "scene": "Arena",
  "state": "Playing",
  "fps": 60,
  "ping_ms": 25,
  "score": "2-1",
  "round": 3,
  "objective": "Checkpoint A",
  "event": "Round started"
}
```

See `Telemetry.cs` for the allowed fields.
