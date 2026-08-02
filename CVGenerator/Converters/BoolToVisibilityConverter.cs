using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CVGenerator.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public virtual object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return ToBool(value) ? Visibility.Visible : Visibility.Collapsed;
    }

    public virtual object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
            return visibility == Visibility.Visible;
        return false;
    }

    protected static bool ToBool(object? value)
    {
        return value switch
        {
            bool b => b,
            string s => !string.IsNullOrWhiteSpace(s),
            null => false,
            _ => true
        };
    }
}

public class InverseBoolToVisibilityConverter : BoolToVisibilityConverter
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return ToBool(value) ? Visibility.Collapsed : Visibility.Visible;
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
            return visibility != Visibility.Visible;
        return false;
    }
}
