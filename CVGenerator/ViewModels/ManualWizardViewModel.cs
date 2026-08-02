using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CVGenerator.Localization;
using CVGenerator.Models;
using CVGenerator.Models.SectionModels;
using CVGenerator.Services;
using CVGenerator.Templates;
using Serilog;

namespace CVGenerator.ViewModels;

public partial class ManualWizardViewModel : ObservableObject
{
    private readonly DraftPersistenceService _drafts;
    private readonly PdfGeneratorService _pdfService;
    private readonly PowerPointGeneratorService _pptService;
    private readonly WordExportService _wordService;
    private readonly IUserDialog _dialog;
    private readonly INavigationService _navigation;
    private readonly LocalizationService _loc = LocalizationService.Instance;
    private readonly DispatcherTimer _autoSaveTimer;

    public Step1PersonalViewModel Step1 { get; }
    public Step2ExperiencesViewModel Step2 { get; }
    public Step3TemplateViewModel Step3 { get; }

    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<WizardStepItem> Steps { get; } = new();

    public ManualWizardViewModel(Step1PersonalViewModel step1,
        Step2ExperiencesViewModel step2,
        DraftPersistenceService drafts,
        PdfGeneratorService pdfService,
        PowerPointGeneratorService pptService,
        WordExportService wordService,
        IUserDialog dialog,
        INavigationService navigation)
    {
        Step1 = step1;
        Step2 = step2;
        _drafts = drafts;
        _pdfService = pdfService;
        _pptService = pptService;
        _wordService = wordService;
        _dialog = dialog;
        _navigation = navigation;

        Step3 = new Step3TemplateViewModel(this);

        BuildStepList();
        RelocalizeSteps();

        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _autoSaveTimer.Tick += (_, _) => SaveDraftInternal();
        _autoSaveTimer.Start();

        _loc.CultureChanged += RelocalizeSteps;
    }

    private void BuildStepList()
    {
        Steps.Clear();
        Steps.Add(new WizardStepItem(1, _loc.GetString("Wizard.Step1Title"), "👤"));
        Steps.Add(new WizardStepItem(2, _loc.GetString("Wizard.Step2Title"), "📄"));
        Steps.Add(new WizardStepItem(3, _loc.GetString("Wizard.Step3Title"), "🖌️"));
    }

    private void RelocalizeSteps()
    {
        var titles = new[]
        {
            _loc.GetString("Wizard.Step1Title"),
            _loc.GetString("Wizard.Step2Title"),
            _loc.GetString("Wizard.Step3Title")
        };
        for (int i = 0; i < Steps.Count && i < titles.Length; i++)
            Steps[i].Title = titles[i];
    }

    partial void OnCurrentStepChanged(int value)
    {
        foreach (var step in Steps)
        {
            step.State = step.Number < value ? WizardStepState.Completed
                : step.Number == value ? WizardStepState.Active
                : WizardStepState.Pending;
        }
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
        Log.Debug("Wizard step changed to {Step}", value);
    }

    public bool CanGoNext => CurrentStep < 3;
    public bool CanGoPrevious => CurrentStep > 1;

    [RelayCommand]
    private void Next()
    {
        if (CurrentStep == 3)
        {
            Step3.ExportPdfCommand.Execute(null);
            return;
        }

        if (CurrentStep == 1)
        {
            Step1.BuildDateOfBirth();
            if (!Step1.ValidateAll())
            {
                StatusMessage = _loc.GetString("Step1.ValidationFirstName");
                return;
            }
        }

        if (CurrentStep < 3)
            CurrentStep++;
    }

    [RelayCommand]
    private void Previous()
    {
        if (CurrentStep > 1)
            CurrentStep--;
    }

    [RelayCommand]
    private void Finish() => Step3.ExportPdfCommand.Execute(null);

    [RelayCommand]
    private void Cancel()
    {
        SaveDraftInternal();
        _navigation.NavigateToLanding();
    }

    [RelayCommand]
    private void SaveDraft() => SaveDraftInternal();

    private void SaveDraftInternal()
    {
        if (IsBusy) return;
        try
        {
            _drafts.SaveDraft(BuildCvData(), Step3.SelectedTemplate.Key);
            StatusMessage = _loc.GetString("Status.Saved");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Auto-save draft failed");
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void LoadDraft() => LoadDraftInternal();

    public bool LoadDraftInternal()
    {
        var (data, templateKey) = _drafts.LoadDraft();
        if (data == null) return false;

        Step1.SetPersonalInfo(data.PersonalInfo);
        Step2.SetCvData(data);
        if (!string.IsNullOrEmpty(templateKey))
        {
            var tpl = TemplateCatalog.All.FirstOrDefault(t => t.Key == templateKey);
            if (tpl != null) Step3.SelectTemplate(tpl);
        }

        StatusMessage = _loc.GetString("Status.DraftResumed");
        return true;
    }

    public CVData BuildCvData()
    {
        Step1.BuildDateOfBirth();
        Step1.ValidateAll();

        var pi = Step1.PersonalInfo;
        pi.FullName = string.IsNullOrWhiteSpace(pi.FullName)
            ? $"{pi.FirstName} {pi.LastName}".Trim()
            : pi.FullName;
        pi.FullNameLatin = string.IsNullOrWhiteSpace(pi.FullNameLatin) && IsLatin(pi.FullName)
            ? pi.FullName
            : pi.FullNameLatin;

        var data = new CVData
        {
            PersonalInfo = pi,
            Summary = GetPlain(Step2.Sections.OfType<ObjectiveSection>().FirstOrDefault()?.Content)
        };

        foreach (var section in Step2.Sections)
        {
            switch (section)
            {
                case WorkExperienceSection we:
                    data.Experience = we.Entries.Select(e => new WorkExperience
                    {
                        Company = e.Employer,
                        Position = e.JobTitle,
                        City = e.City,
                        StartMonth = e.StartMonth,
                        StartYear = e.StartYear,
                        EndMonth = e.EndMonth,
                        EndYear = e.EndYear,
                        IsCurrent = e.IsCurrent,
                        Description = GetPlain(e.Description),
                        StartDate = BuildDate(e.StartMonth, e.StartYear),
                        EndDate = e.IsCurrent ? "Present" : BuildDate(e.EndMonth, e.EndYear)
                    }).ToList();
                    break;

                case EducationSection edu:
                    data.Education = edu.Entries.Select(e => new Education
                    {
                        Institution = e.Institution,
                        Degree = e.Degree,
                        FieldOfStudy = e.FieldOfStudy,
                        City = e.City,
                        StartMonth = e.StartMonth,
                        StartYear = e.StartYear,
                        EndMonth = e.EndMonth,
                        EndYear = e.EndYear,
                        Description = GetPlain(e.Description),
                        Year = e.EndYear
                    }).ToList();
                    break;

                case SkillsSection sk:
                    data.Skills = sk.Entries.Where(s => !string.IsNullOrWhiteSpace(s.Name))
                        .Select(s => new Skill { Name = s.Name, Level = s.Level }).ToList();
                    break;

                case LanguagesSection lg:
                    data.Languages = lg.Entries.Where(l => !string.IsNullOrWhiteSpace(l.Name))
                        .Select(l => new Language { Name = l.Name, Level = l.Level }).ToList();
                    break;

                case InterestsSection it:
                    data.Interests = it.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                    break;

                case ReferencesSection rf:
                    data.References = rf.Entries.Where(r => !string.IsNullOrWhiteSpace(r.Name))
                        .Select(r => new Reference { Name = r.Name, Company = r.Company, Phone = r.Phone, Email = r.Email }).ToList();
                    break;

                case CoursesSection cs:
                    data.Courses = cs.Entries.Where(c => !string.IsNullOrWhiteSpace(c.Name))
                        .Select(c => new Course { Name = c.Name, Institution = c.Institution, Year = c.Year, Description = GetPlain(c.Description) }).ToList();
                    break;

                case AchievementsSection ac:
                    data.Achievements = ac.Entries.Where(a => !string.IsNullOrWhiteSpace(a.Title))
                        .Select(a => new Achievement { Title = a.Title, Date = a.Date, Description = GetPlain(a.Description) }).ToList();
                    break;

                case PublicationsSection pb:
                    data.Publications = pb.Entries.Where(p => !string.IsNullOrWhiteSpace(p.Title))
                        .Select(p => new Publication { Title = p.Title, Publisher = p.Publisher, Date = p.Date, Url = p.Url }).ToList();
                    break;

                case CustomSection custom:
                    data.CustomSections.Add(new CustomSectionData { Title = custom.Title, Content = GetPlain(custom.Content) });
                    break;
            }
        }

        return data;
    }

    public void ExportData(TemplateDefinition template, ExportFormat format)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var data = BuildCvData();
            var fileName = BuildFileName(data, format);
            var outputDir = Path.Combine(Path.GetTempPath(), "CVGenerator", "Exports");
            Directory.CreateDirectory(outputDir);
            var outputPath = Path.Combine(outputDir, fileName);

            switch (format)
            {
                case ExportFormat.Pdf:
                    _pdfService.GeneratePdf(data, template, Step2.GetSections(), outputPath);
                    break;
                case ExportFormat.PowerPoint:
                    _pptService.GenerateCV(data, fileName, template.Key);
                    outputPath = _pptService.LastOutputPath;
                    break;
                case ExportFormat.Word:
                    _wordService.GenerateDocx(data, template, outputPath);
                    break;
            }

            if (File.Exists(outputPath) && _dialog.ShowQuestion($"{_loc.GetString("Status.PdfDone").Replace("{0}", outputPath)}\n\n{_loc.GetString("Common.Yes")} — {_loc.GetString("Common.No")}", "CV Generator"))
            {
                _dialog.OpenFile(outputPath);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            _dialog.ShowInfo(ex.Message, "CV Generator");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string BuildFileName(CVData data, ExportFormat format)
    {
        string first = Sanitize(data.PersonalInfo.FirstName);
        string last = Sanitize(data.PersonalInfo.LastName);
        string ext = format switch
        {
            ExportFormat.Pdf => "pdf",
            ExportFormat.PowerPoint => "pptx",
            _ => "docx"
        };
        return $"CV_{first}_{last}_{DateTime.Now:yyyy-MM-dd}.{ext}";
    }

    private static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "Resume";
        var sanitized = Regex.Replace(input.Trim(), @"[^\p{L}\p{N}\-_]", "");
        return string.IsNullOrEmpty(sanitized) ? "Resume" : sanitized;
    }

    private static string GetPlain(string? xamlOrText) => RichTextHelper.GetPlainText(xamlOrText ?? string.Empty);

    private static string BuildDate(string month, string year) => $"{month} {year}".Trim();

    private static bool IsLatin(string text) => text.All(c => !char.IsLetter(c) || c < 128);
}

public enum WizardStepState
{
    Pending,
    Active,
    Completed
}

public partial class WizardStepItem : ObservableObject
{
    public int Number { get; }

    [ObservableProperty]
    private string _title;

    public string Icon { get; }

    [ObservableProperty]
    private WizardStepState _state;

    public WizardStepItem(int number, string title, string icon)
    {
        Number = number;
        _title = title;
        Icon = icon;
    }
}
