using Microsoft.Extensions.Configuration;
using Serilog;

namespace CVGenerator.Services;

public class AppConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-pro";
    public int MaxTokens { get; set; } = 8192;
    public double Temperature { get; set; } = 0.1;
    public string? DefaultTemplate { get; set; }
    public string OutputDirectory { get; set; } = "Output";
    public List<string> SupportedLanguages { get; set; } = new() { "ar", "fr", "en" };
    public double ConfidenceThreshold { get; set; } = 0.75;
}

public class ConfigurationService
{
    private readonly IConfigurationRoot _configuration;
    public AppConfiguration Settings { get; }

    public ConfigurationService()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        _configuration = builder.Build();
        Settings = new AppConfiguration();

        _configuration.GetSection("Gemini").Bind(Settings);
        _configuration.GetSection("Templates").Bind(Settings);
        _configuration.GetSection("OCR").Bind(Settings);

        Log.Information("Configuration loaded. Model: {Model}, Output: {Output}",
            Settings.Model, Settings.OutputDirectory);
    }

    public T? GetSection<T>(string sectionName) where T : class, new()
    {
        var section = new T();
        _configuration.GetSection(sectionName).Bind(section);
        return section;
    }
}
