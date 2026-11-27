using System.Text.Json.Serialization;

namespace SpaceBattle.Lib;

public class ShipSnapshot
{
    [JsonPropertyName("shipId")]
    public string ShipId { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public int[] Position { get; set; } = Array.Empty<int>();

    [JsonPropertyName("velocity")]
    public int[] Velocity { get; set; } = Array.Empty<int>();

    [JsonPropertyName("direction")]
    public int[] Direction { get; set; } = Array.Empty<int>();

    [JsonPropertyName("fuelLevel")]
    public int FuelLevel { get; set; }

    [JsonPropertyName("fuelConsumption")]
    public int FuelConsumption { get; set; }
}
