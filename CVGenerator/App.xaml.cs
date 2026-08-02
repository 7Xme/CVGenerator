using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CVGenerator.Localization;
using CVGenerator.Services;
using CVGenerator.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace CVGenerator;

public partial class App : Application
{
    private ServiceProvider _serviceProvider = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ConfigureLogging();
        HookGlobalExceptionHandlers();

        Log.Information("=== CV Generator Starting ===");
        Log.Information("Version: {Version}, OS: {Os}, BaseDir: {Dir}",
            typeof(App).Assembly.GetName().Version?.ToString() ?? "unknown",
            Environment.OSVersion.VersionString,
            AppDomain.CurrentDomain.BaseDirectory);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var shell = _serviceProvider.GetRequiredService<ShellViewModel>();
        shell.CurrentViewModel = _serviceProvider.GetRequiredService<LandingViewModel>();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureLogging()
    {
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        try
        {
            Directory.CreateDirectory(logDir);
        }
        catch
        {
            logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CVGenerator", "logs");
            Directory.CreateDirectory(logDir);
        }

        var fileSink = Path.Combine(logDir, "cv_generator_.log");
        var debugSink = Path.Combine(logDir, "cv_generator_debug_.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(fileSink,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true)
            .WriteTo.File(debugSink,
                restrictedToMinimumLevel: LogEventLevel.Debug,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)
            .CreateLogger();

        Log.Information("Log file: {LogDir}", logDir);
    }

    private void HookGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            // Capture WPF data-binding errors (a common silent cause of UI breakage).
            PresentationTraceSources.Refresh();
            var bindingSource = PresentationTraceSources.DataBindingSource;
            bindingSource.Listeners.Clear();
            bindingSource.Listeners.Add(new SerilogTraceListener());
            bindingSource.Switch.Level = SourceLevels.Error;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to attach WPF binding-error tracing");
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "UNHANDLED exception on UI thread");
        e.Handled = true;
        MessageBox.Show(
            "An unexpected error occurred and was saved to the log file. The application will keep running.\n\n" +
            e.Exception.Message,
            "CV Generator - Error",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            Log.Fatal(ex, "UNHANDLED non-UI thread exception (isTerminating={IsTerminating})", e.IsTerminating);
        else
            Log.Fatal("UNHANDLED non-UI thread exception (isTerminating={IsTerminating}): {Object}", e.IsTerminating, e.ExceptionObject);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception");
        e.SetObserved();
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

    /// <summary>
    /// Forwards WPF trace output (e.g. binding errors) into Serilog so that
    /// silent failures are captured in the log file.
    /// </summary>
    private sealed class SerilogTraceListener : TraceListener
    {
        public override void Write(string? message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                Log.Error("WPF trace: {Message}", message.Trim());
        }

        public override void WriteLine(string? message) => Write(message);
    }
}
