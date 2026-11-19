// Example publisher to embed in YOUR game. It writes only the game's own telemetry snapshot.
// Use the same key as Purple's connector field. Shared memory name: PurpleTelemetry_<key>.
using System;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;

namespace PurpleGameTelemetryExample;

public sealed class GameTelemetryPublisher : IDisposable
{
    private readonly MemoryMappedFile _map;
    private readonly MemoryMappedViewAccessor _view;
    private const int Capacity = 4096;

    public GameTelemetryPublisher(string gameKey)
    {
        string key = new string(gameKey.Trim().ToCharArray());
        _map = MemoryMappedFile.CreateOrOpen($"PurpleTelemetry_{key}", Capacity);
        _view = _map.CreateViewAccessor(0, Capacity, MemoryMappedFileAccess.ReadWrite);
    }

    public void Publish(object snapshot)
    {
        // Snapshot should contain only the whitelisted telemetry fields documented in README.
        var json = JsonSerializer.Serialize(snapshot);
        var data = Encoding.UTF8.GetBytes(json);
        if (data.Length >= Capacity) throw new ArgumentException("Telemetry snapshot is too large.");
        _view.Write(0, new byte[Capacity]); // clear stale bytes first
        _view.WriteArray(0, data, 0, data.Length);
    }

    public void Dispose()
    {
        _view.Dispose();
        _map.Dispose();
    }
}

// Example usage inside your game loop:
// using var telemetry = new GameTelemetryPublisher("MyGame");
// telemetry.Publish(new {
//   game = "MyGame", version = "0.1", scene = "Arena", state = "Playing",
//   fps = 60.0, ping_ms = 22, score = "2-1", round = 3,
//   objective = "Checkpoint A", event = "Round started"
// });
