using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CVGenerator.Localization;
using CVGenerator.Models;
using CVGenerator.Services;

namespace CVGenerator.ViewModels;

public partial class Step1PersonalViewModel : ObservableObject
{
    public PersonalInfo PersonalInfo { get; private set; }

    public ObservableCollection<string> Days { get; } = new();
    public ObservableCollection<string> Months { get; } = new();
    public ObservableCollection<string> Years { get; } = new();
    public ObservableCollection<string> GenderOptions { get; } = new();
    public ObservableCollection<string> ResumeLanguageOptions { get; } = new();

    [ObservableProperty]
    private string _selectedDay = string.Empty;

    [ObservableProperty]
    private string _selectedMonth = string.Empty;

    [ObservableProperty]
    private string _selectedYear = string.Empty;

    [ObservableProperty]
    private string _selectedGender = string.Empty;

    [ObservableProperty]
    private string _selectedResumeLanguage = "en";

    [ObservableProperty]
    private string _photoPreviewPath = string.Empty;

    [ObservableProperty]
    private bool _hasPhoto;

    [ObservableProperty]
    private bool _isValidFirstName = true;

    [ObservableProperty]
    private bool _isValidLastName = true;

    [ObservableProperty]
    private bool _isValidEmail = true;

    public Step1PersonalViewModel()
    {
        PersonalInfo = new PersonalInfo();
        InitializeStaticLists();
        RefreshLocalizedLists();

        LocalizationService.Instance.CultureChanged += () =>
        {
            RefreshLocalizedLists();
            ValidateAll();
        };
    }

    public void SetPersonalInfo(PersonalInfo pi)
    {
        PersonalInfo = pi;

        SelectedResumeLanguage = string.IsNullOrEmpty(pi.ResumeLanguage) ? "en" : pi.ResumeLanguage;

        // Populate month dropdown (localized)
        var monthNames = LocalizationService.Instance.GetMonthNames();
        for (int i = 0; i < monthNames.Length; i++) Months[i] = monthNames[i];

        ParseDateOfBirth(pi.DateOfBirth);
        SelectedGender = pi.Gender;
        HasPhoto = pi.PhotoBytes is { Length: > 0 };
        PhotoPreviewPath = pi.PhotoPath;

        ValidateAll();
    }

    private void InitializeStaticLists()
    {
        Days.Clear();
        for (int i = 1; i <= 31; i++) Days.Add(i.ToString());
        Years.Clear();
        for (int year = 2010; year >= 1950; year--) Years.Add(year.ToString());

        GenderOptions.Clear();
        GenderOptions.Add(LocalizationService.Instance.GetString("Step1.GenderMale"));
        GenderOptions.Add(LocalizationService.Instance.GetString("Step1.GenderFemale"));
        GenderOptions.Add(LocalizationService.Instance.GetString("Step1.GenderNotSay"));

        ResumeLanguageOptions.Clear();
        ResumeLanguageOptions.Add("English");
        ResumeLanguageOptions.Add("Français");
        ResumeLanguageOptions.Add("العربية");
    }

    private void RefreshLocalizedLists()
    {
        var monthNames = LocalizationService.Instance.GetMonthNames();
        Months.Clear();
        foreach (var m in monthNames) Months.Add(m);

        GenderOptions.Clear();
        GenderOptions.Add(LocalizationService.Instance.GetString("Step1.GenderMale"));
        GenderOptions.Add(LocalizationService.Instance.GetString("Step1.GenderFemale"));
        GenderOptions.Add(LocalizationService.Instance.GetString("Step1.GenderNotSay"));

        if (SelectedGender.Length > 0)
            SelectedGender = LocalizeGender(SelectedGender);
    }

    private string LocalizeGender(string current)
    {
        if (current == "Male" || current == "Homme" || current == "ذكر") return LocalizationService.Instance.GetString("Step1.GenderMale");
        if (current == "Female" || current == "Femme" || current == "أنثى") return LocalizationService.Instance.GetString("Step1.GenderFemale");
        return LocalizationService.Instance.GetString("Step1.GenderNotSay");
    }

    private void ParseDateOfBirth(string dateOfBirth)
    {
        if (string.IsNullOrWhiteSpace(dateOfBirth)) return;
        var parts = dateOfBirth.Split('-', '/');
        if (parts.Length >= 3)
        {
            if (int.TryParse(parts[2], out var year) && year >= 1950 && year <= 2010) SelectedYear = year.ToString();
            if (int.TryParse(parts[1], out var month) && month >= 1 && month <= 12) SelectedMonth = Months[(month - 1) % 12];
            if (int.TryParse(parts[0], out var day) && day >= 1 && day <= 31) SelectedDay = day.ToString();
        }
    }

    public void BuildDateOfBirth()
    {
        if (int.TryParse(SelectedYear, out var year) && int.TryParse(SelectedMonth.Split(' ').First(), out _))
        {
            int month = Months.IndexOf(SelectedMonth) + 1;
            int day = int.TryParse(SelectedDay, out var d) ? d : 0;
            if (month >= 1 && day >= 1 && year >= 1950)
                PersonalInfo.DateOfBirth = $"{day:00}-{month:00}-{year}";
        }
    }

    public bool ValidateAll()
    {
        IsValidFirstName = !string.IsNullOrWhiteSpace(PersonalInfo.FirstName);
        IsValidLastName = !string.IsNullOrWhiteSpace(PersonalInfo.LastName);
        IsValidEmail = !string.IsNullOrWhiteSpace(PersonalInfo.Email) && IsValidEmailAddress(PersonalInfo.Email);

        return IsValidFirstName && IsValidLastName && IsValidEmail;
    }

    private static bool IsValidEmailAddress(string email) => email.Contains('@') && email.Contains('.');

    partial void OnSelectedResumeLanguageChanged(string value)
    {
        PersonalInfo.ResumeLanguage = value switch
        {
            "Français" => "fr",
            "العربية" => "ar",
            _ => "en"
        };
    }

    partial void OnSelectedGenderChanged(string value)
    {
        PersonalInfo.Gender = value;
    }

    public void LoadPhotoFromFile(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            PersonalInfo.PhotoBytes = bytes;
            PersonalInfo.PhotoPath = path;
            PhotoPreviewPath = path;
            HasPhoto = true;
        }
        catch (Exception)
        {
            // ignore invalid image files
        }
    }

    public void LoadPhotoFromBytes(byte[] bytes)
    {
        PersonalInfo.PhotoBytes = bytes;
        PersonalInfo.PhotoPath = string.Empty;
        HasPhoto = bytes is { Length: > 0 };
    }

    [RelayCommand]
    private void BrowsePhoto()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
            Title = LocalizationService.Instance.GetString("Step1.PhotoBrowse")
        };
        if (dlg.ShowDialog() == true)
            LoadPhotoFromFile(dlg.FileName);
    }
}
