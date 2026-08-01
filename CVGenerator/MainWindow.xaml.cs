using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CVGenerator.Localization;
using CVGenerator.ViewModels;

namespace CVGenerator;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;

    public MainWindow(ShellViewModel shell)
    {
        InitializeComponent();
        _shell = shell;
        DataContext = _shell;

        ApplyLanguageComboSelection();
    }

    private void ApplyLanguageComboSelection()
    {
        var current = LocalizationService.Instance.CurrentCulture.Name;
        foreach (ComboBoxItem item in LanguageCombo.Items)
        {
            if (item.Tag is string tag && tag == current)
            {
                LanguageCombo.SelectedItem = item;
                break;
            }
        }
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageCombo.SelectedItem is ComboBoxItem { Tag: string tag })
            LocalizationService.Instance.SetCulture(tag);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
