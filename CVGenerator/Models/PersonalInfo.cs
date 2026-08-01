using System.Text.Json.Serialization;

namespace CVGenerator.Models;

public class PersonalInfo
{
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("full_name_latin")]
    public string FullNameLatin { get; set; } = string.Empty;

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;

    [JsonPropertyName("phone_primary")]
    public string PhonePrimary { get; set; } = string.Empty;

    [JsonPropertyName("phone_secondary")]
    public string PhoneSecondary { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("national_id")]
    public string NationalId { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("zip_code")]
    public string ZipCode { get; set; } = string.Empty;

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("place_of_birth")]
    public string PlaceOfBirth { get; set; } = string.Empty;

    [JsonPropertyName("driving_license")]
    public string DrivingLicense { get; set; } = string.Empty;

    [JsonPropertyName("date_of_birth")]
    public string DateOfBirth { get; set; } = string.Empty;

    [JsonPropertyName("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonPropertyName("resume_language")]
    public string ResumeLanguage { get; set; } = "en";

    [JsonPropertyName("photo_path")]
    public string PhotoPath { get; set; } = string.Empty;

    [JsonIgnore]
    public byte[]? PhotoBytes { get; set; }

    public string DisplayFullName =>
        !string.IsNullOrWhiteSpace(FullName) ? FullName
        : string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? FullNameLatin
            : string.Join(" ", new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
