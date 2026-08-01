using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CVGenerator.ViewModels;
using Microsoft.Win32;

namespace CVGenerator.Views;

public partial class Step1PersonalView : UserControl
{
    private Step1PersonalViewModel ViewModel => (Step1PersonalViewModel)DataContext;

    public Step1PersonalView()
    {
        InitializeComponent();
    }

    private void PhotoArea_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.BrowsePhotoCommand.Execute(null);
    }
}
