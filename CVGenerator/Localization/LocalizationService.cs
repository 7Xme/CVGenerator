using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows;

namespace CVGenerator.Localization;

/// <summary>
/// Singleton localization service. Exposes an indexer so XAML can bind
/// via the LocExtension markup extension and update live on culture switch.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    public static LocalizationService Instance { get; } = new();

    private CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");
    private readonly ResourceManager _resources = new("CVGenerator.Localization.Resources",
        typeof(LocalizationService).Assembly);

    private LocalizationService() { }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action? CultureChanged;

    public CultureInfo CurrentCulture
    {
        get => _culture;
        set => SetCulture(value);
    }

    public string this[string key]
    {
        get
        {
            string value = _resources.GetString(key, _culture) ?? _resources.GetString(key, CultureInfo.InvariantCulture) ?? key;
            return value;
        }
    }

    public bool IsRtl => _culture.TextInfo.IsRightToLeft;

    public FlowDirection WindowFlowDirection => IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public void SetCulture(string cultureName) => SetCulture(CultureInfo.GetCultureInfo(cultureName));

    public void SetCulture(CultureInfo culture)
    {
        if (culture == null) return;

        _culture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // PropertyChanged(null) signals "all properties changed".
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        CultureChanged?.Invoke();
    }

    public string GetString(string key) => this[key];

    public string GetString(string key, string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return GetString(key);

        try
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            return _resources.GetString(key, culture)
                ?? _resources.GetString(key, CultureInfo.InvariantCulture)
                ?? key;
        }
        catch (CultureNotFoundException)
        {
            return GetString(key);
        }
    }

    public string GetMonthName(int month)
    {
        var months = _culture.DateTimeFormat.MonthNames;
        return months[(month - 1) % 12];
    }

    public string[] GetMonthNames()
    {
        return _culture.DateTimeFormat.MonthNames.Take(12).ToArray();
    }
}
