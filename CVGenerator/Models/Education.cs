using System.Text.Json.Serialization;

namespace CVGenerator.Models;

public class Education
{
    [JsonPropertyName("degree")]
    public string Degree { get; set; } = string.Empty;

    [JsonPropertyName("institution")]
    public string Institution { get; set; } = string.Empty;

    [JsonPropertyName("field_of_study")]
    public string FieldOfStudy { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public string Year { get; set; } = string.Empty;

    [JsonPropertyName("start_month")]
    public string StartMonth { get; set; } = string.Empty;

    [JsonPropertyName("start_year")]
    public string StartYear { get; set; } = string.Empty;

    [JsonPropertyName("end_month")]
    public string EndMonth { get; set; } = string.Empty;

    [JsonPropertyName("end_year")]
    public string EndYear { get; set; } = string.Empty;

    [JsonPropertyName("mention")]
    public string Mention { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
