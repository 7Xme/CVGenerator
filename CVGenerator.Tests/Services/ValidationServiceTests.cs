using CVGenerator.Models;
using CVGenerator.Services;
using FluentAssertions;
using Xunit;

namespace CVGenerator.Tests.Services;

public class ValidationServiceTests
{
    private readonly ValidationService _sut = new();

    [Fact]
    public void Validate_Should_Return_Error_For_Null_Data()
    {
        var errors = _sut.Validate(null!);

        errors.Should().Contain(e => e.Contains("فارغة"));
    }

    [Fact]
    public void Validate_Should_Return_Error_For_Empty_Name()
    {
        var data = new CVData();

        var errors = _sut.Validate(data);

        errors.Should().Contain(e => e.Contains("الاسم"));
    }

    [Fact]
    public void Validate_Should_Return_Error_For_Invalid_Email()
    {
        var data = new CVData
        {
            PersonalInfo = new PersonalInfo
            {
                FullName = "Test",
                Email = "invalid-email",
                PhonePrimary = "+212 600-000000"
            }
        };

        var errors = _sut.Validate(data);

        errors.Should().Contain(e => e.Contains("غير صالح"));
    }

    [Fact]
    public void Validate_Should_Pass_For_Valid_Data()
    {
        var data = new CVData
        {
            PersonalInfo = new PersonalInfo
            {
                FullName = "أمينة أمغيمة",
                Email = "amina@example.com",
                PhonePrimary = "+212 600-000000"
            }
        };

        var errors = _sut.Validate(data);

        errors.Should().NotContain(e => e.Contains("مطلوب"));
    }

    [Fact]
    public void Validate_Should_Warn_About_Missing_Institution()
    {
        var data = new CVData
        {
            PersonalInfo = new PersonalInfo
            {
                FullName = "Test",
                Email = "test@test.com",
                PhonePrimary = "+212 600-000000"
            },
            Education = new List<Education>
            {
                new() { Degree = "Master", Institution = "" }
            }
        };

        var errors = _sut.Validate(data);

        errors.Should().Contain(e => e.Contains("بدون مؤسسة"));
    }

    [Fact]
    public void Validate_Should_Warn_About_Missing_Company()
    {
        var data = new CVData
        {
            PersonalInfo = new PersonalInfo
            {
                FullName = "Test",
                Email = "test@test.com",
                PhonePrimary = "+212 600-000000"
            },
            Experience = new List<WorkExperience>
            {
                new() { Company = "", Position = "Dev" }
            }
        };

        var errors = _sut.Validate(data);

        errors.Should().Contain(e => e.Contains("بدون اسم شركة"));
    }
}
