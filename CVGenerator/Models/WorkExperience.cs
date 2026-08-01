using System.Text.Json.Serialization;

namespace CVGenerator.Models;

public class WorkExperience
{
    [JsonPropertyName("company")]
    public string Company { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("start_date")]
    public string StartDate { get; set; } = string.Empty;

    [JsonPropertyName("end_date")]
    public string EndDate { get; set; } = string.Empty;

    [JsonPropertyName("start_month")]
    public string StartMonth { get; set; } = string.Empty;

    [JsonPropertyName("start_year")]
    public string StartYear { get; set; } = string.Empty;

    [JsonPropertyName("end_month")]
    public string EndMonth { get; set; } = string.Empty;

    [JsonPropertyName("end_year")]
    public string EndYear { get; set; } = string.Empty;

    [JsonPropertyName("is_current")]
    public bool IsCurrent { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("tasks")]
    public List<string> Tasks { get; set; } = new();
}
