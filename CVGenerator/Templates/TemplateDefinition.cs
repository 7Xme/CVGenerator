namespace CVGenerator.Templates;

public enum TemplateLayout
{
    Standard,
    Sidebar
}

public class TemplateDefinition
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = "#5C6BC0";
    public string SecondaryColor { get; set; } = "#37474F";
    public string AccentColor { get; set; } = "#FFB74D";
    public string FontFamily { get; set; } = "Segoe UI";
    public TemplateLayout Layout { get; set; } = TemplateLayout.Standard;
    public string Description { get; set; } = string.Empty;
}

public static class TemplateCatalog
{
    public static IReadOnlyList<TemplateDefinition> All { get; } = new List<TemplateDefinition>
    {
        new() { Key = "auckland", Name = "Auckland", PrimaryColor = "#5C6BC0", SecondaryColor = "#3949AB", AccentColor = "#FFB74D", Layout = TemplateLayout.Sidebar },
        new() { Key = "edinburgh", Name = "Edinburgh", PrimaryColor = "#2980B9", SecondaryColor = "#1A5276", AccentColor = "#D4AC0D", Layout = TemplateLayout.Standard },
        new() { Key = "princeton", Name = "Princeton", PrimaryColor = "#E67E22", SecondaryColor = "#A04000", AccentColor = "#5C6BC0", Layout = TemplateLayout.Sidebar },
        new() { Key = "otago", Name = "Otago", PrimaryColor = "#27AE60", SecondaryColor = "#145A32", AccentColor = "#F1C40F", Layout = TemplateLayout.Standard },
        new() { Key = "berkeley", Name = "Berkeley", PrimaryColor = "#C0392B", SecondaryColor = "#78281F", AccentColor = "#F39C12", Layout = TemplateLayout.Sidebar },
        new() { Key = "harvard", Name = "Harvard", PrimaryColor = "#A93226", SecondaryColor = "#641E16", AccentColor = "#D4AC0D", Layout = TemplateLayout.Standard },
        new() { Key = "stanford", Name = "Stanford", PrimaryColor = "#8E44AD", SecondaryColor = "#512E5F", AccentColor = "#F39C12", Layout = TemplateLayout.Standard },
        new() { Key = "cambridge", Name = "Cambridge", PrimaryColor = "#1F618D", SecondaryColor = "#154360", AccentColor = "#D4AC0D", Layout = TemplateLayout.Standard },
        new() { Key = "oxford", Name = "Oxford", PrimaryColor = "#1C2833", SecondaryColor = "#0E6251", AccentColor = "#B7950B", Layout = TemplateLayout.Sidebar }
    };

    public static TemplateDefinition Default => All[0];
}
