using System.IO;
using System.Windows;
using System.Windows.Input;
using CVGenerator.ViewModels;
using Microsoft.Win32;

namespace CVGenerator;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void ImageArea_Click(object sender, MouseButtonEventArgs e)
    {
        _viewModel.BrowseImageCommand.Execute(null);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
