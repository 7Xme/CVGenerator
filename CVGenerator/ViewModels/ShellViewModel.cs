using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;

namespace CVGenerator.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _currentViewModel;

    partial void OnCurrentViewModelChanged(object? value)
    {
        Log.Debug("Current view changed to {View}", value?.GetType().Name ?? "null");
    }
}
