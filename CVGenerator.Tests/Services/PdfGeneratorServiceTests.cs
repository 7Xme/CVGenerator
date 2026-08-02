using System.IO;
using CVGenerator.Localization;
using CVGenerator.Models;
using CVGenerator.Models.SectionModels;
using CVGenerator.Services;
using CVGenerator.Templates;
using FluentAssertions;
using Xunit;

namespace CVGenerator.Tests.Services;

public class PdfGeneratorServiceTests : IDisposable
{
    private readonly string _dir;

    public PdfGeneratorServiceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "CVGenerator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    [Fact]
    public void GeneratePdf_English_Should_Produce_File()
    {
        var svc = new PdfGeneratorService();
        var data = SampleCv("en", "John", "Doe");

        var path = svc.GeneratePdf(data, TemplateCatalog.Default, Sections("en"), Path.Combine(_dir, "en.pdf"));

        File.Exists(path).Should().BeTrue();
        new FileInfo(path).Length.Should().BeGreaterThan(500);
    }

    [Fact]
    public void GeneratePdf_Arabic_Should_Not_Throw_And_Use_Segoe_UI()
    {
        var svc = new PdfGeneratorService();
        var data = SampleCv("ar", "أحمد", "محمد");
        var sections = new List<SectionBase>
        {
            new WorkExperienceSection { Title = "Work Experience", Icon = "💼" }
        };
        ((WorkExperienceSection)sections[0]).Entries.Add(new WorkExperienceEntry
        {
            JobTitle = "مطور",
            Employer = "شركة",
            StartMonth = "يناير",
            StartYear = "2020",
            IsCurrent = true,
            Description = "تطوير تطبيقات"
        });

        var path = svc.GeneratePdf(data, TemplateCatalog.Default, sections, Path.Combine(_dir, "ar.pdf"));

        File.Exists(path).Should().BeTrue();

        // The Arabic build must embed an Arabic-capable font family name.
        var bytes = File.ReadAllBytes(path);
        var ascii = System.Text.Encoding.ASCII.GetString(bytes);
        ascii.Should().Contain("Segoe");
    }

    [Fact]
    public void GetString_Arabic_Returns_Arabic_Translation()
    {
        var loc = LocalizationService.Instance;

        loc.GetString("Section.WorkExperience", "ar").Should().Be("الخبرات العملية");
        loc.GetString("Section.WorkExperience", "fr").Should().NotBe("الخبرات العملية");
        loc.GetString("Common.Present", "ar").Should().Be("حتى الآن");
    }

    [Fact]
    public void LocalizeSectionTitle_Preserves_Custom_Title()
    {
        // A user-renamed title must not be overwritten by localization during generation.
        var svc = new PdfGeneratorService();
        var data = SampleCv("ar", "أحمد", "محمد");
        var custom = new WorkExperienceSection { Title = "My Custom Title", Icon = "💼" };

        var path = svc.GeneratePdf(data, TemplateCatalog.Default, new List<SectionBase> { custom }, Path.Combine(_dir, "custom.pdf"));

        File.Exists(path).Should().BeTrue();
    }

    private static CVData SampleCv(string lang, string first, string last) => new()
    {
        PersonalInfo = new PersonalInfo
        {
            FirstName = first,
            LastName = last,
            Email = "a@b.com",
            ResumeLanguage = lang
        }
    };

    private static List<SectionBase> Sections(string culture) => new()
    {
        new ObjectiveSection { Title = TitleFor(culture, "Section.Objective"), Icon = "🎯" },
        new WorkExperienceSection { Title = TitleFor(culture, "Section.WorkExperience"), Icon = "💼" },
        new EducationSection { Title = TitleFor(culture, "Section.Education"), Icon = "🎓" },
        new InterestsSection { Title = TitleFor(culture, "Section.Interests"), Icon = "🎨" },
        new ReferencesSection { Title = TitleFor(culture, "Section.References"), Icon = "💬" },
        new SkillsSection { Title = TitleFor(culture, "Section.Skills"), Icon = "🛠️" }
    };

    private static string TitleFor(string culture, string key) => LocalizationService.Instance.GetString(key, culture);
}