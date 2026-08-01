using System.Text.Json.Serialization;

namespace CVGenerator.Models;

/// <summary>
/// Represents the raw response from the Gemini API.
/// </summary>
public class GeminiApiResponse
{
    [JsonPropertyName("candidates")]
    public List<Candidate> Candidates { get; set; } = new();
}

public class Candidate
{
    [JsonPropertyName("content")]
    public Content Content { get; set; } = new();

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
}

public class Content
{
    [JsonPropertyName("parts")]
    public List<Part> Parts { get; set; } = new();
}

public class Part
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// The structured CV response parsed from Gemini text output.
/// </summary>
public class GeminiCVResponse
{
    [JsonPropertyName("cv_data")]
    public CVData? CVData { get; set; }

    [JsonPropertyName("metadata")]
    public Metadata? Metadata { get; set; }
}

public class Metadata
{
    [JsonPropertyName("confidence_score")]
    public double ConfidenceScore { get; set; }

    [JsonPropertyName("fields_detected")]
    public List<string> FieldsDetected { get; set; } = new();

    [JsonPropertyName("suggestions")]
    public List<string> Suggestions { get; set; } = new();

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();
}
