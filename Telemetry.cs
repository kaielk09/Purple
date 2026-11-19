using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PurpleConnector;

public sealed class Telemetry
{
    [JsonPropertyName("game")] public string Game { get; set; } = "";
    [JsonPropertyName("version")] public string Version { get; set; } = "";
    [JsonPropertyName("scene")] public string Scene { get; set; } = "";
    [JsonPropertyName("state")] public string State { get; set; } = "";
    [JsonPropertyName("fps")] public double Fps { get; set; }
    [JsonPropertyName("ping_ms")] public int PingMs { get; set; }
    [JsonPropertyName("score")] public string Score { get; set; } = "";
    [JsonPropertyName("round")] public int Round { get; set; }
    [JsonPropertyName("objective")] public string Objective { get; set; } = "";
    [JsonPropertyName("event")] public string Event { get; set; } = "";
    [JsonPropertyName("timestamp_utc")] public DateTimeOffset TimestampUtc { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Game)
            && Game.Length <= 80 && Version.Length <= 40 && Scene.Length <= 100
            && State.Length <= 40 && Score.Length <= 40 && Objective.Length <= 160
            && Event.Length <= 200 && double.IsFinite(Fps) && Fps >= 0 && Fps <= 1000
            && PingMs >= 0 && PingMs <= 60000 && Round >= 0 && Round <= 100000
            && TimestampUtc != default;
    }

    public bool IsFresh(TimeSpan? maxAge = null)
    {
        var age = DateTimeOffset.UtcNow - TimestampUtc;
        return age >= TimeSpan.FromSeconds(-10) && age <= (maxAge ?? TimeSpan.FromSeconds(5));
    }

    public static Telemetry? Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Length > 4096) return null;
        try
        {
            var value = JsonSerializer.Deserialize<Telemetry>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return value is not null && value.IsValid() ? value : null;
        }
        catch (JsonException) { return null; }
    }
}
