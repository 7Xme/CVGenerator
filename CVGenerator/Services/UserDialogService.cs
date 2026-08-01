using System.Diagnostics;
using System.Windows;
using CVGenerator.ViewModels;

namespace CVGenerator.Services;

public interface IUserDialog
{
    void ShowInfo(string message, string? title = null);
    bool ShowQuestion(string message, string? title = null);
    string? ShowInput(string message, string title, string? initial = null);
    void OpenFile(string path);
    void OpenFolder(string path);
}

public class UserDialogService : IUserDialog
{
    public void ShowInfo(string message, string? title = null)
    {
        MessageBox.Show(message, title ?? "CV Generator", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public bool ShowQuestion(string message, string? title = null)
    {
        return MessageBox.Show(message, title ?? "CV Generator", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    public string? ShowInput(string message, string title, string? initial = null)
    {
        var vm = new InputDialogViewModel { Title = title, Message = message, Text = initial ?? string.Empty };
        var dialog = new Views.InputDialog { DataContext = vm };
        return dialog.ShowDialog() == true ? vm.Text : null;
    }

    public void OpenFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch { }
    }

    public void OpenFolder(string path)
    {
        try
        {
            var dir = System.IO.Directory.Exists(path) ? path : System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
        catch { }
    }
}
