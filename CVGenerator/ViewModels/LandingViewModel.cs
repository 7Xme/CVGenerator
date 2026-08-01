using CommunityToolkit.Mvvm.Input;
using CVGenerator.Services;

namespace CVGenerator.ViewModels;

public partial class LandingViewModel
{
    private readonly INavigationService _navigation;

    public LandingViewModel(INavigationService navigation)
    {
        _navigation = navigation;
    }

    [RelayCommand]
    private void LaunchAi() => _navigation.NavigateTo<MainViewModel>();

    [RelayCommand]
    private void LaunchManual() => _navigation.NavigateTo<ManualWizardViewModel>();
}
