using System.Windows.Data;
using System.Windows.Markup;

namespace CVGenerator.Localization;

/// <summary>
/// XAML markup extension:  {loc:Loc Key=Landing.Title}  or  {loc:Loc Landing.Title}
/// Returns a one-way binding to the LocalizationService indexer so text
/// updates immediately when the UI culture changes.
/// </summary>
[MarkupExtensionReturnType(typeof(object))]
public class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocExtension() { }

    public LocExtension(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(Key))
            return string.Empty;

        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay
        };

        // Return the binding directly when used as a target value (common case).
        return binding.ProvideValue(serviceProvider);
    }
}
