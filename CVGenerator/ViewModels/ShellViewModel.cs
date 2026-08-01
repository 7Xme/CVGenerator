using CommunityToolkit.Mvvm.ComponentModel;

namespace CVGenerator.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private object? _currentViewModel;
}
