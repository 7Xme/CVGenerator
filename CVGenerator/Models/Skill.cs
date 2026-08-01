using System.Text.Json.Serialization;

namespace CVGenerator.Models;

public class Skill
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("level")]
    public string Level { get; set; } = string.Empty;
}
