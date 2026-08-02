using CVGenerator.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CVGenerator.Services;

public interface INavigationService
{
    void NavigateTo<TViewModel>(Action<TViewModel>? configure = null) where TViewModel : class;
    void NavigateToLanding();
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ShellViewModel _shell;

    public NavigationService(IServiceProvider serviceProvider, ShellViewModel shell)
    {
        _serviceProvider = serviceProvider;
        _shell = shell;
    }

    public void NavigateTo<TViewModel>(Action<TViewModel>? configure = null) where TViewModel : class
    {
        var vm = _serviceProvider.GetRequiredService<TViewModel>();
        configure?.Invoke(vm);
        _shell.CurrentViewModel = vm;
        Log.Debug("Navigated to {ViewModel}", typeof(TViewModel).Name);
    }

    public void NavigateToLanding()
    {
        _shell.CurrentViewModel = _serviceProvider.GetRequiredService<LandingViewModel>();
        Log.Debug("Navigated to Landing");
    }
}
