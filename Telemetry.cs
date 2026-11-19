using System.Text.Json;

namespace PurpleConnector;

public sealed class Telemetry
{
    public string Game { get; set; } = "";
    public string Version { get; set; } = "";
    public string Scene { get; set; } = "";
    public string State { get; set; } = "";
    public double Fps { get; set; }
    public int PingMs { get; set; }
    public string Score { get; set; } = "";
    public int Round { get; set; }
    public string Objective { get; set; } = "";
    public string Event { get; set; } = "";

    public static Telemetry? Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Telemetry>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException) { return null; }
    }
}
