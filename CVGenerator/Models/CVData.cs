using System.Text.Json.Serialization;
using CVGenerator.Models.SectionModels;

namespace CVGenerator.Models;

public class CVData
{
    [JsonPropertyName("personal_info")]
    public PersonalInfo PersonalInfo { get; set; } = new();

    [JsonPropertyName("education")]
    public List<Education> Education { get; set; } = new();

    [JsonPropertyName("experience")]
    public List<WorkExperience> Experience { get; set; } = new();

    [JsonPropertyName("skills")]
    public List<Skill> Skills { get; set; } = new();

    [JsonPropertyName("languages")]
    public List<Language> Languages { get; set; } = new();

    [JsonPropertyName("interests")]
    public List<string> Interests { get; set; } = new();

    [JsonPropertyName("references")]
    public List<Reference> References { get; set; } = new();

    [JsonPropertyName("courses")]
    public List<Course> Courses { get; set; } = new();

    [JsonPropertyName("achievements")]
    public List<Achievement> Achievements { get; set; } = new();

    [JsonPropertyName("publications")]
    public List<Publication> Publications { get; set; } = new();

    [JsonPropertyName("custom_sections")]
    public List<CustomSectionData> CustomSections { get; set; } = new();

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;
}
