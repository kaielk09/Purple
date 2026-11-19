// Embed this publisher in YOUR game. It exposes only the game's own summary telemetry.
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PurpleGameTelemetryExample;

public sealed class GameTelemetryPublisher : IDisposable
{
    private const int Capacity = 4096;
    private readonly MemoryMappedFile _map;
    private readonly MemoryMappedViewAccessor _view;
    private readonly object _gate = new();
    private bool _disposed;

    public GameTelemetryPublisher(string gameKey)
    {
        var key = new string((gameKey ?? "").Trim()
            .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
        if (key.Length == 0 || key.Length > 64)
            throw new ArgumentException("Game key must contain 1–64 letters, digits, '_' or '-'.", nameof(gameKey));

        _map = MemoryMappedFile.CreateOrOpen($"PurpleTelemetry_{key}", Capacity);
        _view = _map.CreateViewAccessor(0, Capacity, MemoryMappedFileAccess.ReadWrite);
    }

    public void Publish(GameTelemetrySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            snapshot.TimestampUtc = DateTimeOffset.UtcNow;
            var json = JsonSerializer.Serialize(snapshot);
            var data = Encoding.UTF8.GetBytes(json);
            if (data.Length >= Capacity)
                throw new ArgumentException("Telemetry snapshot is too large.", nameof(snapshot));

            // Clear first, write UTF-8 JSON, then flush so readers never treat old trailing bytes as current data.
            _view.Write(0, new byte[Capacity], 0, Capacity);
            _view.WriteArray(0, data, 0, data.Length);
            _view.Flush();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _view.Write(0, new byte[Capacity], 0, Capacity);
            _view.Flush();
            _view.Dispose();
            _map.Dispose();
        }
    }
}

public sealed class GameTelemetrySnapshot
{
    [JsonPropertyName("game")] public string Game { get; set; } = "MyGame";
    [JsonPropertyName("version")] public string Version { get; set; } = "1.0";
    [JsonPropertyName("scene")] public string Scene { get; set; } = "";
    [JsonPropertyName("state")] public string State { get; set; } = "";
    [JsonPropertyName("fps")] public double Fps { get; set; }
    [JsonPropertyName("ping_ms")] public int PingMs { get; set; }
    [JsonPropertyName("score")] public string Score { get; set; } = "";
    [JsonPropertyName("round")] public int Round { get; set; }
    [JsonPropertyName("objective")] public string Objective { get; set; } = "";
    [JsonPropertyName("event")] public string Event { get; set; } = "";
    [JsonPropertyName("timestamp_utc")] public DateTimeOffset TimestampUtc { get; set; }
}

// Example:
// using var publisher = new GameTelemetryPublisher("MyGame");
// publisher.Publish(new GameTelemetrySnapshot { Game = "MyGame", Scene = "Arena", State = "Playing", Fps = 60, PingMs = 22 });
