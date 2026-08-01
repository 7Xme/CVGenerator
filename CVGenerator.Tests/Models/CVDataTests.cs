using CVGenerator.Models;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace CVGenerator.Tests.Models;

public class CVDataTests
{
    [Fact]
    public void CVData_Should_Have_Default_Empty_Collections()
    {
        var data = new CVData();

        data.PersonalInfo.Should().NotBeNull();
        data.Education.Should().BeEmpty();
        data.Experience.Should().BeEmpty();
        data.Skills.Should().BeEmpty();
        data.Languages.Should().BeEmpty();
        data.Interests.Should().BeEmpty();
        data.Summary.Should().BeEmpty();
    }

    [Fact]
    public void PersonalInfo_Should_Have_Default_Empty_Strings()
    {
        var pi = new PersonalInfo();

        pi.FullName.Should().BeEmpty();
        pi.Email.Should().BeEmpty();
        pi.PhonePrimary.Should().BeEmpty();
    }

    [Fact]
    public void CVData_Should_Serialize_And_Deserialize_Correctly()
    {
        var data = new CVData
        {
            PersonalInfo = new PersonalInfo
            {
                FullName = "أمينة أمغيمة",
                Email = "amina@example.com",
                PhonePrimary = "+212 600-000000"
            },
            Education = new List<Education>
            {
                new() { Degree = "Master", Institution = "جامعة الحسن الثاني", Year = "2024" }
            },
            Skills = new List<Skill>
            {
                new() { Name = "C#", Level = "Expert" }
            },
            Languages = new List<Language>
            {
                new() { Name = "العربية", Level = "اللغة الأم" }
            },
            Summary = "مطور برمجيات"
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        json.Should().NotBeNullOrEmpty();

        var deserialized = JsonSerializer.Deserialize<CVData>(json);
        deserialized.Should().NotBeNull();
        deserialized!.PersonalInfo.FullName.Should().Be("أمينة أمغيمة");
        deserialized.PersonalInfo.Email.Should().Be("amina@example.com");
        deserialized.Education.Should().HaveCount(1);
        deserialized.Skills.Should().HaveCount(1);
        deserialized.Languages.Should().HaveCount(1);
        deserialized.Summary.Should().Be("مطور برمجيات");
    }

    [Fact]
    public void WorkExperience_Should_Handle_Tasks_Correctly()
    {
        var exp = new WorkExperience
        {
            Company = "شركة مثال",
            Position = "مطور",
            StartDate = "2024-01",
            EndDate = "الآن",
            Tasks = new List<string> { "تطوير تطبيقات", "إدارة قواعد البيانات" }
        };

        exp.Tasks.Should().HaveCount(2);
        exp.Tasks[0].Should().Be("تطوير تطبيقات");
        exp.Tasks[1].Should().Be("إدارة قواعد البيانات");
    }

    [Fact]
    public void GeminiCVResponse_Should_Have_Nullable_CVData()
    {
        var response = new GeminiCVResponse
        {
            CVData = null,
            Metadata = new Metadata { ConfidenceScore = 0.0, Warnings = new List<string> { "فارغ" } }
        };

        response.CVData.Should().BeNull();
        response.Metadata.Should().NotBeNull();
        response.Metadata!.ConfidenceScore.Should().Be(0.0);
    }

    [Fact]
    public void Metadata_Should_Have_Default_Values()
    {
        var meta = new Metadata();

        meta.ConfidenceScore.Should().Be(0.0);
        meta.FieldsDetected.Should().BeEmpty();
        meta.Suggestions.Should().BeEmpty();
        meta.Warnings.Should().BeEmpty();
    }
}
