using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CVGenerator.ViewModels;
using Microsoft.Win32;
using Serilog;

namespace CVGenerator.Views;

public partial class Step1PersonalView : UserControl
{
    public Step1PersonalView()
    {
        InitializeComponent();
    }

    private void PhotoArea_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is Step1PersonalViewModel vm)
            vm.BrowsePhotoCommand.Execute(null);
    }

    private void RequiredField_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is not Step1PersonalViewModel vm)
            return;

        try
        {
            vm.ValidateAll();
        }
        catch (Exception ex)
        {
            var name = (sender as FrameworkElement)?.Name ?? "?";
            Log.Error(ex, "Validation failed while typing in field '{Field}'", name);
        }
    }
}
