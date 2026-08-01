using System.IO;
using System.Windows;
using CVGenerator.Localization;
using CVGenerator.Services;
using CVGenerator.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CVGenerator;

public partial class App : Application
{
    private ServiceProvider _serviceProvider = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/cv_generator_.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        Log.Information("=== CV Generator Starting ===");

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var shell = _serviceProvider.GetRequiredService<ShellViewModel>();
        shell.CurrentViewModel = _serviceProvider.GetRequiredService<LandingViewModel>();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var config = new ConfigurationService();

        services.AddSingleton(config);
        services.AddSingleton(config.Settings);

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<AppConfiguration>();
            return new GeminiOCRService(settings.ApiKey, settings.Model);
        });

        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<AppConfiguration>();
            var templatePath = settings.DefaultTemplate;
            if (templatePath != null && !Path.IsPathRooted(templatePath))
            {
                templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, templatePath);
            }
            return new PowerPointGeneratorService(templatePath, settings.OutputDirectory);
        });

        services.AddSingleton<ValidationService>();
        services.AddSingleton<PrintService>();

        services.AddSingleton(LocalizationService.Instance);
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
        services.AddSingleton<IUserDialog, UserDialogService>();
        services.AddSingleton<DraftPersistenceService>();
        services.AddSingleton<PdfGeneratorService>();
        services.AddSingleton<WordExportService>();

        services.AddTransient<LandingViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<Step1PersonalViewModel>();
        services.AddTransient<Step2ExperiencesViewModel>();
        services.AddTransient<ManualWizardViewModel>();

        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("=== CV Generator Shutting Down ===");
        Log.CloseAndFlush();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
