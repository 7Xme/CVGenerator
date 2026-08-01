using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CVGenerator.Localization;
using CVGenerator.Models;
using CVGenerator.Models.SectionModels;

namespace CVGenerator.ViewModels;

public partial class Step2ExperiencesViewModel : ObservableObject
{
    public ObservableCollection<SectionBase> Sections { get; } = new();

    public ObservableCollection<string> SkillLevels { get; } = new();

    [ObservableProperty]
    private string _newTagText = string.Empty;

    private readonly LocalizationService _loc = LocalizationService.Instance;

    public Step2ExperiencesViewModel()
    {
        CreateDefaultSections();
        RefreshSkillLevels();
        _loc.CultureChanged += RelocalizeSectionTitles;
        _loc.CultureChanged += RefreshSkillLevels;
    }

    private void RefreshSkillLevels()
    {
        SkillLevels.Clear();
        SkillLevels.Add(_loc.GetString("Step2.SkillLevelBeginner"));
        SkillLevels.Add(_loc.GetString("Step2.SkillLevelIntermediate"));
        SkillLevels.Add(_loc.GetString("Step2.SkillLevelAdvanced"));
    }

    public void SetCvData(CVData data)
    {
        // If a draft provided education/experience/skills/languages, fold them into sections.
        if (data.Education.Count > 0)
        {
            var edu = Sections.OfType<EducationSection>().FirstOrDefault();
            if (edu != null)
            {
                edu.Entries.Clear();
                foreach (var e in data.Education)
                {
                    edu.Entries.Add(new EducationEntry
                    {
                        Institution = e.Institution,
                        Degree = e.Degree,
                        FieldOfStudy = e.FieldOfStudy,
                        City = e.City,
                        StartMonth = e.StartMonth,
                        StartYear = e.StartYear,
                        EndMonth = e.EndMonth,
                        EndYear = e.EndYear,
                        Description = e.Description
                    });
                }
            }
        }

        if (data.Experience.Count > 0)
        {
            var we = Sections.OfType<WorkExperienceSection>().FirstOrDefault();
            if (we != null)
            {
                we.Entries.Clear();
                foreach (var e in data.Experience)
                {
                    we.Entries.Add(new WorkExperienceEntry
                    {
                        JobTitle = e.Position,
                        Employer = e.Company,
                        City = e.City,
                        StartMonth = e.StartMonth,
                        StartYear = e.StartYear,
                        EndMonth = e.EndMonth,
                        EndYear = e.EndYear,
                        IsCurrent = e.IsCurrent,
                        Description = e.Description
                    });
                }
            }
        }

        if (data.Skills.Count > 0)
        {
            var sk = Sections.OfType<SkillsSection>().FirstOrDefault();
            if (sk != null)
            {
                sk.Entries.Clear();
                foreach (var s in data.Skills)
                    sk.Entries.Add(new SkillEntry { Name = s.Name, Level = s.Level });
            }
        }

        if (data.Languages.Count > 0)
        {
            var lg = Sections.OfType<LanguagesSection>().FirstOrDefault();
            if (lg != null)
            {
                lg.Entries.Clear();
                foreach (var l in data.Languages)
                    lg.Entries.Add(new LanguageEntry { Name = l.Name, Level = l.Level });
            }
        }

        if (data.Interests.Count > 0)
        {
            var it = Sections.OfType<InterestsSection>().FirstOrDefault();
            if (it != null)
            {
                it.Tags.Clear();
                foreach (var i in data.Interests) it.Tags.Add(i);
            }
        }

        if (data.CustomSections.Count > 0)
        {
            foreach (var cs in data.CustomSections)
            {
                var custom = new CustomSection { Title = cs.Title, Content = cs.Content };
                Sections.Add(custom);
            }
        }
    }

    private void CreateDefaultSections()
    {
        var objective = new ObjectiveSection { Title = _loc.GetString("Section.Objective"), Icon = "🎯" };
        var work = new WorkExperienceSection { Title = _loc.GetString("Section.WorkExperience"), Icon = "💼" };
        var edu = new EducationSection { Title = _loc.GetString("Section.Education"), Icon = "🎓" };
        var interests = new InterestsSection { Title = _loc.GetString("Section.Interests"), Icon = "🎨" };
        var refs = new ReferencesSection { Title = _loc.GetString("Section.References"), Icon = "💬" };
        var skills = new SkillsSection { Title = _loc.GetString("Section.Skills"), Icon = "🛠️" };

        int order = 0;
        foreach (var s in new SectionBase[] { objective, work, edu, interests, refs, skills })
            s.DisplayOrder = order++;

        Sections.Add(objective);
        Sections.Add(work);
        Sections.Add(edu);
        Sections.Add(interests);
        Sections.Add(refs);
        Sections.Add(skills);
    }

    private void RelocalizeSectionTitles()
    {
        foreach (var s in Sections)
        {
            s.Title = s.Kind switch
            {
                SectionKind.ResumeObjective => _loc.GetString("Section.Objective"),
                SectionKind.WorkExperience => _loc.GetString("Section.WorkExperience"),
                SectionKind.Education => _loc.GetString("Section.Education"),
                SectionKind.Interests => _loc.GetString("Section.Interests"),
                SectionKind.References => _loc.GetString("Section.References"),
                SectionKind.Skills => _loc.GetString("Section.Skills"),
                SectionKind.Languages => _loc.GetString("Section.Languages"),
                SectionKind.Courses => _loc.GetString("Section.Courses"),
                SectionKind.Achievements => _loc.GetString("Section.Achievements"),
                SectionKind.Publications => _loc.GetString("Section.Publications"),
                _ => s.Title
            };
        }
    }

    // ===== Add section commands =====

    [RelayCommand]
    private void AddLanguages()
    {
        var section = new LanguagesSection { Title = _loc.GetString("Section.Languages"), Icon = "🌍" };
        section.Entries.Add(new LanguageEntry());
        Sections.Add(section);
    }

    [RelayCommand]
    private void AddCourses()
    {
        var section = new CoursesSection { Title = _loc.GetString("Section.Courses"), Icon = "📚" };
        section.Entries.Add(new CourseEntry());
        Sections.Add(section);
    }

    [RelayCommand]
    private void AddAchievements()
    {
        var section = new AchievementsSection { Title = _loc.GetString("Section.Achievements"), Icon = "🏆" };
        section.Entries.Add(new AchievementEntry());
        Sections.Add(section);
    }

    [RelayCommand]
    private void AddPublications()
    {
        var section = new PublicationsSection { Title = _loc.GetString("Section.Publications"), Icon = "📄" };
        section.Entries.Add(new PublicationEntry());
        Sections.Add(section);
    }

    [RelayCommand]
    private void AddCustom()
    {
        var input = new InputDialogViewModel
        {
            Title = _loc.GetString("Step2.AddCustom"),
            Message = _loc.GetString("Step2.CustomTitle")
        };
        var dialog = new Views.InputDialog { DataContext = input };
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(input.Text))
        {
            var section = new CustomSection { Title = input.Text.Trim(), Icon = "📝" };
            Sections.Add(section);
        }
    }

    // ===== Section operations =====

    [RelayCommand]
    private void AddEntry(SectionBase section)
    {
        switch (section)
        {
            case WorkExperienceSection we: we.Entries.Add(new WorkExperienceEntry()); break;
            case EducationSection edu: edu.Entries.Add(new EducationEntry()); break;
            case SkillsSection sk: sk.Entries.Add(new SkillEntry()); break;
            case LanguagesSection lg: lg.Entries.Add(new LanguageEntry()); break;
            case ReferencesSection rf: rf.Entries.Add(new ReferenceEntry()); break;
            case CoursesSection cs: cs.Entries.Add(new CourseEntry()); break;
            case AchievementsSection ac: ac.Entries.Add(new AchievementEntry()); break;
            case PublicationsSection pb: pb.Entries.Add(new PublicationEntry()); break;
        }
    }

    [RelayCommand]
    private void RemoveEntry(object? entry)
    {
        if (entry == null) return;
        foreach (var section in Sections)
        {
            switch (section)
            {
                case WorkExperienceSection we when we.Entries.Contains((WorkExperienceEntry)entry): we.Entries.Remove((WorkExperienceEntry)entry); return;
                case EducationSection edu when edu.Entries.Contains((EducationEntry)entry): edu.Entries.Remove((EducationEntry)entry); return;
                case SkillsSection sk when sk.Entries.Contains((SkillEntry)entry): sk.Entries.Remove((SkillEntry)entry); return;
                case LanguagesSection lg when lg.Entries.Contains((LanguageEntry)entry): lg.Entries.Remove((LanguageEntry)entry); return;
                case ReferencesSection rf when rf.Entries.Contains((ReferenceEntry)entry): rf.Entries.Remove((ReferenceEntry)entry); return;
                case CoursesSection cs when cs.Entries.Contains((CourseEntry)entry): cs.Entries.Remove((CourseEntry)entry); return;
                case AchievementsSection ac when ac.Entries.Contains((AchievementEntry)entry): ac.Entries.Remove((AchievementEntry)entry); return;
                case PublicationsSection pb when pb.Entries.Contains((PublicationEntry)entry): pb.Entries.Remove((PublicationEntry)entry); return;
            }
        }
    }

    [RelayCommand]
    private void RemoveTag(object? tag)
    {
        if (tag is not string value) return;
        foreach (var section in Sections.OfType<InterestsSection>())
        {
            if (section.Tags.Contains(value))
            {
                section.Tags.Remove(value);
                return;
            }
        }
    }

    [RelayCommand]
    private void AddTag(SectionBase? section)
    {
        if (section is not InterestsSection it) return;
        var value = NewTagText?.Trim();
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (!it.Tags.Contains(value)) it.Tags.Add(value);
            NewTagText = string.Empty;
        }
    }

    [RelayCommand]
    private void ToggleVisibility(SectionBase section) => section.IsVisibleInCv = !section.IsVisibleInCv;

    [RelayCommand]
    private void ToggleCollapse(SectionBase section) => section.IsCollapsed = !section.IsCollapsed;

    [RelayCommand]
    private void RenameSection(SectionBase section)
    {
        var input = new InputDialogViewModel
        {
            Title = _loc.GetString("Step2.Rename"),
            Message = _loc.GetString("Step2.CustomTitle"),
            Text = section.Title
        };
        var dialog = new Views.InputDialog { DataContext = input };
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(input.Text))
            section.Title = input.Text.Trim();
    }

    [RelayCommand]
    private void DeleteSection(SectionBase section) => Sections.Remove(section);

    [RelayCommand]
    private void MoveUp(SectionBase section)
    {
        int index = Sections.IndexOf(section);
        if (index > 0) Sections.Move(index, index - 1);
    }

    [RelayCommand]
    private void MoveDown(SectionBase section)
    {
        int index = Sections.IndexOf(section);
        if (index >= 0 && index < Sections.Count - 1) Sections.Move(index, index + 1);
    }

    public void MoveSection(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Sections.Count) return;
        if (toIndex < 0) toIndex = 0;
        if (toIndex >= Sections.Count) toIndex = Sections.Count - 1;
        Sections.Move(fromIndex, toIndex);
    }

    public IReadOnlyList<SectionBase> GetSections() => Sections.ToList();
}

public partial class InputDialogViewModel : ObservableObject
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    [ObservableProperty]
    private string _text = string.Empty;
}
