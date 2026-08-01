using CommunityToolkit.Mvvm.ComponentModel;

namespace CVGenerator.Models.SectionModels;

public enum SectionKind
{
    ResumeObjective,
    WorkExperience,
    Education,
    Interests,
    References,
    Skills,
    Languages,
    Courses,
    Achievements,
    Publications,
    Custom
}

public abstract partial class SectionBase : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public SectionKind Kind { get; set; }
    public string Icon { get; set; } = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isCollapsed;

    [ObservableProperty]
    private bool _isVisibleInCv = true;

    [ObservableProperty]
    private int _displayOrder;
}

public partial class ObjectiveSection : SectionBase
{
    [ObservableProperty]
    private string _content = string.Empty;

    public ObjectiveSection()
    {
        Kind = SectionKind.ResumeObjective;
    }
}

public partial class InterestsSection : SectionBase
{
    public ObservableCollection<string> Tags { get; set; } = new();

    public InterestsSection()
    {
        Kind = SectionKind.Interests;
    }
}

public partial class CustomSection : SectionBase
{
    [ObservableProperty]
    private string _content = string.Empty;

    public CustomSection()
    {
        Kind = SectionKind.Custom;
    }
}
