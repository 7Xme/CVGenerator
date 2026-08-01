using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CVGenerator.ViewModels;
using Microsoft.Win32;

namespace CVGenerator.Views;

public partial class AiModeView : UserControl
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public AiModeView()
    {
        InitializeComponent();
    }

    private void ImageArea_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.BrowseImageCommand.Execute(null);
    }

    private void txtApiKey_Loaded(object sender, RoutedEventArgs e)
    {
        // Keep a reference to the PasswordBox for the ViewModel to read.
        ViewModel.AttachPasswordBox(txtApiKey);
    }
}
