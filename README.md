# Purple

Purple is a Windows desktop companion for **telemetry explicitly published by your own game**. It does not inject into a game, inspect arbitrary process memory, or read player/entity positions.

## Features

- Dark purple Windows desktop connector.
- Connects to a named shared-memory telemetry channel.
- Floating, draggable, always-on-top Purple dashboard appears only after a valid, fresh snapshot arrives.
- Dashboard displays game, version, scene, FPS, ping and score.
- Visual switches, dropdown and sliders are interactive but presentation-only; they do not issue game commands.
- Disconnect closes the floating dashboard and returns to the connector.
- Heartbeat timestamp detects stale telemetry.
- Windows CI builds the app and uploads a build artifact.

## Requirements

- Windows 10/11 x64.
- .NET 8 SDK to build locally.
- A game you control with the telemetry publisher integrated.

## Build and run

```powershell
dotnet restore PurpleConnector.csproj
dotnet run --project PurpleConnector.csproj
```

To publish a Windows x64 build:

```powershell
dotnet publish PurpleConnector.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

The GitHub Actions workflow also builds on Windows for pushes and pull requests. Download the `Purple-win-x64` artifact from the workflow run.

## Connect

1. Integrate `GameTelemetryPublisherExample.cs` into your own game project.
2. Create a publisher using a key such as `MyGame`.
3. Publish a `GameTelemetrySnapshot` regularly (recommended every 0.5–1 second).
4. Start Purple and enter the same key.
5. Click **Connect**. The dashboard appears after Purple receives valid, fresh data.
6. Click **Disconnect** on the dashboard to close it and return to the connector.

The key is not an EXE filename. It is the shared-memory channel key. The channel name is `PurpleTelemetry_<key>`.

## Telemetry protocol

The shared-memory block contains UTF-8 JSON, up to 4095 bytes, with a null terminator. Example:

```json
{
  "game": "MyGame",
  "version": "1.0",
  "scene": "Arena",
  "state": "Playing",
  "fps": 60,
  "ping_ms": 25,
  "score": "2-1",
  "round": 3,
  "objective": "Checkpoint A",
  "event": "Round started",
  "timestamp_utc": "2026-11-19T21:00:00+00:00"
}
```

The publisher sets `timestamp_utc` automatically. Purple rejects malformed, out-of-range or stale snapshots. If the publisher stops updating, the dashboard reports that it is waiting for game telemetry.

## Safety and scope

Allowed fields are game name/version, scene, match state, FPS, ping, score, round, objective, event and timestamp. Player/enemy positions, entity lists, aim assistance, targeting and game-action commands are not part of this project. The overlay controls only change their own visible state.
