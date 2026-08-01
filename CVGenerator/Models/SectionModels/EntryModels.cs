using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CVGenerator.Models.SectionModels;

// ===== Section container types (polymorphic) =====

public partial class ObjectiveSection : SectionBase
{
    public ObjectiveSection() { Kind = SectionKind.ResumeObjective; }

    [ObservableProperty]
    private string _content = string.Empty;
}

public partial class InterestsSection : SectionBase
{
    public InterestsSection() { Kind = SectionKind.Interests; }

    public ObservableCollection<string> Tags { get; set; } = new();
}

public partial class CustomSection : SectionBase
{
    public CustomSection() { Kind = SectionKind.Custom; }

    [ObservableProperty]
    private string _content = string.Empty;
}

public partial class WorkExperienceSection : SectionBase
{
    public WorkExperienceSection() { Kind = SectionKind.WorkExperience; }

    public ObservableCollection<WorkExperienceEntry> Entries { get; set; } = new();
}

public partial class EducationSection : SectionBase
{
    public EducationSection() { Kind = SectionKind.Education; }

    public ObservableCollection<EducationEntry> Entries { get; set; } = new();
}

public partial class SkillsSection : SectionBase
{
    public SkillsSection() { Kind = SectionKind.Skills; }

    public ObservableCollection<SkillEntry> Entries { get; set; } = new();
}

public partial class LanguagesSection : SectionBase
{
    public LanguagesSection() { Kind = SectionKind.Languages; }

    public ObservableCollection<LanguageEntry> Entries { get; set; } = new();
}

public partial class ReferencesSection : SectionBase
{
    public ReferencesSection() { Kind = SectionKind.References; }

    public ObservableCollection<ReferenceEntry> Entries { get; set; } = new();
}

public partial class CoursesSection : SectionBase
{
    public CoursesSection() { Kind = SectionKind.Courses; }

    public ObservableCollection<CourseEntry> Entries { get; set; } = new();
}

public partial class AchievementsSection : SectionBase
{
    public AchievementsSection() { Kind = SectionKind.Achievements; }

    public ObservableCollection<AchievementEntry> Entries { get; set; } = new();
}

public partial class PublicationsSection : SectionBase
{
    public PublicationsSection() { Kind = SectionKind.Publications; }

    public ObservableCollection<PublicationEntry> Entries { get; set; } = new();
}

// ===== Entry types (editable rows inside sections) =====

public partial class WorkExperienceEntry : ObservableObject
{
    [ObservableProperty]
    private string _jobTitle = string.Empty;

    [ObservableProperty]
    private string _employer = string.Empty;

    [ObservableProperty]
    private string _city = string.Empty;

    [ObservableProperty]
    private string _startMonth = string.Empty;

    [ObservableProperty]
    private string _startYear = string.Empty;

    [ObservableProperty]
    private string _endMonth = string.Empty;

    [ObservableProperty]
    private string _endYear = string.Empty;

    [ObservableProperty]
    private bool _isCurrent;

    [ObservableProperty]
    private string _description = string.Empty;
}

public partial class EducationEntry : ObservableObject
{
    [ObservableProperty]
    private string _institution = string.Empty;

    [ObservableProperty]
    private string _degree = string.Empty;

    [ObservableProperty]
    private string _fieldOfStudy = string.Empty;

    [ObservableProperty]
    private string _city = string.Empty;

    [ObservableProperty]
    private string _startMonth = string.Empty;

    [ObservableProperty]
    private string _startYear = string.Empty;

    [ObservableProperty]
    private string _endMonth = string.Empty;

    [ObservableProperty]
    private string _endYear = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;
}

public partial class SkillEntry : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _level = string.Empty;
}

public partial class LanguageEntry : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _level = string.Empty;
}

public partial class ReferenceEntry : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _company = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;
}

public partial class CourseEntry : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _institution = string.Empty;

    [ObservableProperty]
    private string _year = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;
}

public partial class AchievementEntry : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _date = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;
}

public partial class PublicationEntry : ObservableObject
{
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _publisher = string.Empty;

    [ObservableProperty]
    private string _date = string.Empty;

    [ObservableProperty]
    private string _url = string.Empty;
}

// ===== Serialization-friendly data contracts (persisted in CVData / SQLite) =====

public class Reference
{
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class Course
{
    public string Name { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class Achievement
{
    public string Title { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class Publication
{
    public string Title { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public class CustomSectionData
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
