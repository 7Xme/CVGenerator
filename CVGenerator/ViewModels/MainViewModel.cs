using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CVGenerator.Models;
using CVGenerator.Services;
using Microsoft.Win32;
using Serilog;

namespace CVGenerator.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly GeminiOCRService _ocrService;
    private readonly PowerPointGeneratorService _pptService;
    private readonly ValidationService _validationService;
    private readonly PrintService _printService;

    private byte[]? _currentImageBytes;

    public MainViewModel(GeminiOCRService ocrService, PowerPointGeneratorService pptService,
        ValidationService validationService, PrintService printService)
    {
        _ocrService = ocrService;
        _pptService = pptService;
        _validationService = validationService;
        _printService = printService;

        _statusMessage = "جاهز";
        _isProcessing = false;
        _canGeneratePpt = false;
        _canPrint = false;
        _progressValue = 0;
    }

    // ==================== Observable Properties ====================

    [ObservableProperty]
    private string _statusMessage;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private bool _canGeneratePpt;

    [ObservableProperty]
    private bool _canPrint;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string? _imagePreviewPath;

    // CV Data
    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _fullNameLatin = string.Empty;

    [ObservableProperty]
    private string _phonePrimary = string.Empty;

    [ObservableProperty]
    private string _phoneSecondary = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _nationalId = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _drivingLicense = string.Empty;

    [ObservableProperty]
    private string _summary = string.Empty;

    [ObservableProperty]
    private string _skillsText = string.Empty;

    [ObservableProperty]
    private string _interestsText = string.Empty;

    [ObservableProperty]
    private double _confidenceScore;

    [ObservableProperty]
    private string _warningsText = string.Empty;

    // Collections
    public ObservableCollection<Education> EducationList { get; } = new();
    public ObservableCollection<WorkExperience> ExperienceList { get; } = new();
    public ObservableCollection<Language> LanguageList { get; } = new();
    public ObservableCollection<Skill> SkillList { get; } = new();

    // Internal state
    private string? _lastGeneratedPptPath;
    private CVData? _currentCvData;

    // ==================== Commands ====================

    [RelayCommand]
    private async Task BrowseImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "اختيار صورة النموذج اليدوي",
            Filter = "صور مدعومة|*.jpg;*.jpeg;*.png;*.bmp;*.tiff;*.pdf|جميع الملفات|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            _currentImageBytes = await File.ReadAllBytesAsync(dialog.FileName);
            ImagePreviewPath = dialog.FileName;
            StatusMessage = $"تم تحميل الصورة: {Path.GetFileName(dialog.FileName)} ({_currentImageBytes.Length / 1024} KB)";
            Log.Information("Image loaded: {Path}", dialog.FileName);
        }
    }

    [RelayCommand]
    private async Task ProcessImage()
    {
        if (_currentImageBytes == null)
        {
            StatusMessage = "⚠️ الرجاء اختيار صورة أولاً";
            return;
        }

        IsProcessing = true;
        ProgressValue = 0;
        StatusMessage = "جاري معالجة الصورة وإرسالها إلى Gemini...";
        CanGeneratePpt = false;
        CanPrint = false;

        try
        {
            ProgressValue = 30;
            var result = await _ocrService.ExtractCVFromImageAsync(_currentImageBytes);
            ProgressValue = 70;

            if (result.CVData == null)
            {
                StatusMessage = "⚠️ لم يتم التعرف على بيانات في الصورة";
                WarningsText = string.Join("\n", result.Metadata?.Warnings ?? new List<string>());
                return;
            }

            _currentCvData = result.CVData;
            PopulateUI(result.CVData, result.Metadata);

            var validationErrors = _validationService.Validate(result.CVData);
            if (validationErrors.Count > 0)
            {
                StatusMessage = $"✅ تم استخراج البيانات مع {validationErrors.Count} تحذير";
                WarningsText = string.Join("\n", validationErrors);
            }
            else
            {
                StatusMessage = $"✅ تم استخراج البيانات بنجاح (الثقة: {result.Metadata?.ConfidenceScore:P1})";
            }

            ConfidenceScore = result.Metadata?.ConfidenceScore ?? 0;
            CanGeneratePpt = true;
            ProgressValue = 100;
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ: {ex.Message}";
            Log.Error(ex, "Image processing failed");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task GeneratePowerPoint()
    {
        if (_currentCvData == null)
        {
            StatusMessage = "⚠️ لا توجد بيانات للتصدير";
            return;
        }

        IsProcessing = true;
        StatusMessage = "جاري إنشاء PowerPoint...";

        try
        {
            SyncUIToData();

            var outputPath = _pptService.GenerateCV(_currentCvData);
            _lastGeneratedPptPath = outputPath;

            StatusMessage = $"✅ تم إنشاء الملف: {Path.GetFileName(outputPath)}";
            CanPrint = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ في إنشاء PowerPoint: {ex.Message}";
            Log.Error(ex, "PPT generation failed");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void PrintPreview()
    {
        if (_lastGeneratedPptPath == null || !File.Exists(_lastGeneratedPptPath))
        {
            StatusMessage = "⚠️ لا يوجد ملف للطباعة";
            return;
        }

        try
        {
            _printService.OpenPowerPoint(_lastGeneratedPptPath);
            StatusMessage = "✅ تم فتح PowerPoint للمعاينة";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private void PrintDirect()
    {
        if (_lastGeneratedPptPath == null || !File.Exists(_lastGeneratedPptPath))
        {
            StatusMessage = "⚠️ لا يوجد ملف للطباعة";
            return;
        }

        try
        {
            _printService.PrintPowerPoint(_lastGeneratedPptPath);
            StatusMessage = "✅ تم إرسال الملف للطباعة";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ خطأ: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportData()
    {
        if (_currentCvData == null)
        {
            StatusMessage = "⚠️ لا توجد بيانات للتصدير";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "تصدير البيانات كـ JSON",
            Filter = "JSON Files|*.json",
            DefaultExt = ".json",
            FileName = $"CV_Data_{DateTime.Now:yyyyMMdd}.json"
        };

        if (dialog.ShowDialog() == true)
        {
            SyncUIToData();
            var json = JsonSerializer.Serialize(_currentCvData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(dialog.FileName, json);
            StatusMessage = $"✅ تم تصدير البيانات إلى {Path.GetFileName(dialog.FileName)}";
        }
    }

    [RelayCommand]
    private void LoadSampleData()
    {
        _currentCvData = SampleData.Create();
        PopulateUI(_currentCvData, null);
        StatusMessage = "✅ تم تحميل بيانات تجريبية";
        CanGeneratePpt = true;
    }

    [RelayCommand]
    private void ClearAll()
    {
        _currentImageBytes = null;
        _currentCvData = null;
        _lastGeneratedPptPath = null;

        FullName = FullNameLatin = PhonePrimary = PhoneSecondary = string.Empty;
        Email = NationalId = Address = DrivingLicense = string.Empty;
        Summary = SkillsText = InterestsText = WarningsText = string.Empty;
        ConfidenceScore = 0;
        ImagePreviewPath = null;

        EducationList.Clear();
        ExperienceList.Clear();
        LanguageList.Clear();
        SkillList.Clear();

        CanGeneratePpt = false;
        CanPrint = false;
        ProgressValue = 0;
        StatusMessage = "تم مسح الكل";
    }

    // ==================== Private Helpers ====================

    private void PopulateUI(CVData data, Metadata? metadata)
    {
        var pi = data.PersonalInfo;

        FullName = pi.FullName;
        FullNameLatin = pi.FullNameLatin;
        PhonePrimary = pi.PhonePrimary;
        PhoneSecondary = pi.PhoneSecondary;
        Email = pi.Email;
        NationalId = pi.NationalId;
        Address = pi.Address;
        DrivingLicense = pi.DrivingLicense;
        Summary = data.Summary;

        EducationList.Clear();
        foreach (var edu in data.Education)
            EducationList.Add(edu);

        ExperienceList.Clear();
        foreach (var exp in data.Experience)
            ExperienceList.Add(exp);

        SkillList.Clear();
        foreach (var skill in data.Skills)
            SkillList.Add(skill);

        LanguageList.Clear();
        foreach (var lang in data.Languages)
            LanguageList.Add(lang);

        SkillsText = string.Join(", ", data.Skills.Select(s =>
            string.IsNullOrEmpty(s.Level) ? s.Name : $"{s.Name} ({s.Level})"));

        InterestsText = string.Join(", ", data.Interests);

        if (metadata != null)
        {
            ConfidenceScore = metadata.ConfidenceScore;
            WarningsText = string.Join("\n", metadata.Warnings);
            if (metadata.Suggestions.Count > 0)
                WarningsText += "\n\nاقتراحات:\n" + string.Join("\n", metadata.Suggestions);
        }
    }

    private void SyncUIToData()
    {
        if (_currentCvData == null) return;

        _currentCvData.PersonalInfo.FullName = FullName;
        _currentCvData.PersonalInfo.FullNameLatin = FullNameLatin;
        _currentCvData.PersonalInfo.PhonePrimary = PhonePrimary;
        _currentCvData.PersonalInfo.PhoneSecondary = PhoneSecondary;
        _currentCvData.PersonalInfo.Email = Email;
        _currentCvData.PersonalInfo.NationalId = NationalId;
        _currentCvData.PersonalInfo.Address = Address;
        _currentCvData.PersonalInfo.DrivingLicense = DrivingLicense;
        _currentCvData.Summary = Summary;

        _currentCvData.Education = EducationList.ToList();
        _currentCvData.Experience = ExperienceList.ToList();
        _currentCvData.Skills = SkillList.ToList();
        _currentCvData.Languages = LanguageList.ToList();
    }
}

internal static class SampleData
{
    public static CVData Create()
    {
        return new CVData
        {
            PersonalInfo = new PersonalInfo
            {
                FullName = "أمينة أمغيمة",
                FullNameLatin = "AMINA AMGHMIMA",
                PhonePrimary = "+212 6XX-XXXXXX",
                PhoneSecondary = "+212 5XX-XXXXXX",
                Email = "amina.amghmima@example.com",
                NationalId = "XX-XXXXXX",
                Address = "الدار البيضاء، المغرب",
                DrivingLicense = "صنف ب"
            },
            Education = new List<Education>
            {
                new() { Degree = "Master", Institution = "جامعة الحسن الثاني", Year = "2024", Mention = "Bien" },
                new() { Degree = "Licence", Institution = "جامعة الحسن الثاني", Year = "2022", Mention = "Assez Bien" },
                new() { Degree = "Baccalauréat", Institution = "ثانوية ...", Year = "2019", Mention = "Bien" }
            },
            Experience = new List<WorkExperience>
            {
                new()
                {
                    Company = "شركة مثال",
                    Position = "مطور برمجيات",
                    StartDate = "2024-01",
                    EndDate = "الآن",
                    Tasks = new List<string>
                    {
                        "تطوير تطبيقات ويب باستخدام ASP.NET Core",
                        "إدارة قواعد البيانات SQL Server",
                        "العمل ضمن فريق Agile"
                    }
                }
            },
            Skills = new List<Skill>
            {
                new() { Name = "C#", Level = "Expert" },
                new() { Name = "ASP.NET Core", Level = "Advanced" },
                new() { Name = "SQL Server", Level = "Advanced" },
                new() { Name = "Python", Level = "Intermediate" }
            },
            Languages = new List<Language>
            {
                new() { Name = "العربية", Level = "اللغة الأم" },
                new() { Name = "الفرنسية", Level = "Courant" },
                new() { Name = "الإنجليزية", Level = "Moyen" }
            },
            Summary = "مطور برمجيات شغوف بتقنيات .NET، لدي خبرة في تطوير تطبيقات الويب وسطح المكتب. أسعى لتطوير مهاراتي في مجال الذكاء الاصطناعي."
        };
    }
}
