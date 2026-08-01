using System.Globalization;
using CVGenerator.Models;
using CVGenerator.Models.SectionModels;
using CVGenerator.Templates;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Serilog;

namespace CVGenerator.Services;

public class PdfGeneratorService
{
    private static readonly string[] ProficiencyLevels = { "Beginner", "Intermediate", "Advanced", "Expert", "Native" };

    public PdfGeneratorService()
    {
        // QuestPDF 2024.3.0 ships under the MIT license; no license key required.
    }

    public string GeneratePdf(CVData data, TemplateDefinition template,
        IReadOnlyList<SectionBase> sections, string outputPath)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var culture = GetResumeCulture(data.PersonalInfo.ResumeLanguage);
            bool isRtl = culture.TextInfo.IsRightToLeft;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(text => text
                        .FontFamily(template.FontFamily)
                        .FontSize(10)
                        .FontColor("#37474F"));

                    page.Header().Column(header =>
                    {
                        header.Spacing(6);
                        RenderHeader(header, data, template, isRtl);
                    });

                    page.Content().PaddingTop(12).Column(column =>
                    {
                        column.Spacing(10);
                        foreach (var section in sections)
                        {
                            if (!section.IsVisibleInCv) continue;
                            RenderSection(column, section, template, isRtl);
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span(PageNumbers.Current.ToString()).FontColor("#90A4AE").FontSize(8);
                    });
                });
            }).GeneratePdf(outputPath);

            Log.Information("PDF generated: {Path}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate PDF");
            throw new InvalidOperationException($"فشل توليد PDF: {ex.Message}", ex);
        }
    }

    private static void RenderHeader(ColumnDescriptor header, CVData data, TemplateDefinition template, bool isRtl)
    {
        var pi = data.PersonalInfo;
        string name = string.IsNullOrWhiteSpace(pi.FullName)
            ? $"{pi.FirstName} {pi.LastName}".Trim()
            : pi.FullName;

        if (!string.IsNullOrWhiteSpace(pi.FullNameLatin) && !IsArabic(pi.ResumeLanguage))
        {
            header.Item().Text(text =>
            {
                text.Span(name).FontSize(26).Bold().FontColor(template.PrimaryColor);
            });
            header.Item().Text(text =>
            {
                text.Span(pi.FullNameLatin).FontSize(16).FontColor("#78909C");
            });
        }
        else
        {
            header.Item().Text(text =>
            {
                text.Span(name).FontSize(26).Bold().FontColor(template.PrimaryColor);
            });
        }

        var contact = new List<string>();
        if (!string.IsNullOrWhiteSpace(pi.PhonePrimary)) contact.Add(pi.PhonePrimary);
        if (!string.IsNullOrWhiteSpace(pi.Email)) contact.Add(pi.Email);
        if (!string.IsNullOrWhiteSpace(pi.Address)) contact.Add(pi.Address);
        if (!string.IsNullOrWhiteSpace(pi.City))
            contact.Add(string.IsNullOrWhiteSpace(pi.ZipCode) ? pi.City : $"{pi.ZipCode}, {pi.City}");

        if (contact.Count > 0)
        {
            header.Item().PaddingTop(4).Text(text =>
            {
                for (int i = 0; i < contact.Count; i++)
                {
                    if (i > 0) text.Span("  |  ").FontColor("#B0BEC5");
                    text.Span(contact[i]).FontColor("#546E7A").FontSize(9);
                }
            });
        }

        if (pi.PhotoBytes is { Length: > 0 })
        {
            try
            {
                header.Item().PaddingTop(4).MaxWidth(60).MaxHeight(72).Image(pi.PhotoBytes).FitWidth();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not embed photo in PDF");
            }
        }

        header.Item().PaddingTop(6).LineHorizontal(1).LineColor(template.PrimaryColor);
    }

    private static void RenderSection(ColumnDescriptor column, SectionBase section, TemplateDefinition template, bool isRtl)
    {
        if (string.IsNullOrWhiteSpace(section.Title)) return;

        column.Item().PaddingTop(4).Column(item =>
        {
            item.Spacing(4);

            item.Item().Text(text =>
            {
                text.Span(section.Icon + "  ").FontSize(11);
                text.Span(section.Title).FontSize(13).Bold().FontColor(template.SecondaryColor);
            });
            item.Item().LineHorizontal(0.8f).LineColor(template.PrimaryColor);

            switch (section)
            {
                case ObjectiveSection obj:
                    AddParagraph(item, obj.Content);
                    break;

                case InterestsSection ints:
                    AddInlineList(item, ints.Tags.ToList());
                    break;

                case CustomSection custom:
                    AddParagraph(item, custom.Content);
                    break;

                case WorkExperienceSection we:
                    foreach (var e in we.Entries)
                    {
                        RenderEntry(item, BuildList(e.JobTitle, e.Employer, e.City),
                            BuildDateRange(e.StartMonth, e.StartYear, e.EndMonth, e.EndYear, e.IsCurrent),
                            e.Description, isRtl);
                    }
                    break;

                case EducationSection edu:
                    foreach (var e in edu.Entries)
                    {
                        RenderEntry(item, BuildList(e.Degree, e.Institution, e.FieldOfStudy, e.City),
                            BuildDateRange(e.StartMonth, e.StartYear, e.EndMonth, e.EndYear, false),
                            e.Description, isRtl);
                    }
                    break;

                case SkillsSection skills:
                    AddTwoColumn(item, skills.Entries
                        .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                        .Select(s => (s.Name, Level(s.Level))).ToList());
                    break;

                case LanguagesSection langs:
                    AddTwoColumn(item, langs.Entries
                        .Where(l => !string.IsNullOrWhiteSpace(l.Name))
                        .Select(l => (l.Name, Level(l.Level))).ToList());
                    break;

                case ReferencesSection refs:
                    foreach (var r in refs.Entries.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
                    {
                        AddParagraph(item, BuildList(r.Name, r.Company, r.Phone, r.Email));
                    }
                    break;

                case CoursesSection courses:
                    foreach (var c in courses.Entries.Where(c => !string.IsNullOrWhiteSpace(c.Name)))
                    {
                        RenderEntry(item, BuildList(c.Name, c.Institution), c.Year, c.Description, isRtl);
                    }
                    break;

                case AchievementsSection ach:
                    foreach (var a in ach.Entries.Where(a => !string.IsNullOrWhiteSpace(a.Title)))
                    {
                        RenderEntry(item, a.Title, a.Date, a.Description, isRtl);
                    }
                    break;

                case PublicationsSection pubs:
                    foreach (var p in pubs.Entries.Where(p => !string.IsNullOrWhiteSpace(p.Title)))
                    {
                        RenderEntry(item, BuildList(p.Title, p.Publisher), p.Date, p.Url, isRtl);
                    }
                    break;
            }
        });
    }

    private static void RenderEntry(ColumnDescriptor column, string title, string dateRange, string description, bool isRtl)
    {
        column.Item().Column(item =>
        {
            item.Spacing(1);
            item.Item().Row(row =>
            {
                row.RelativeItem().Text(text => text.Span(title).FontSize(10.5f).Bold().FontColor("#263238"));
                if (!string.IsNullOrWhiteSpace(dateRange))
                {
                    row.ConstantItem(140).AlignRight().Text(text =>
                        text.Span(dateRange).FontSize(9).FontColor(templateHint("date")));
                }
            });
            AddParagraph(item, description);
        });
    }

    private static string templateHint(string _) => "#78909C";

    private static void AddParagraph(ColumnDescriptor column, string content)
    {
        var text = RichTextHelper.GetPlainText(content);
        if (string.IsNullOrWhiteSpace(text)) return;

        column.Item().PaddingTop(2).Text(text).FontSize(9.5f).LineHeight(1.25f);
    }

    private static void AddInlineList(ColumnDescriptor column, List<string> items)
    {
        if (items.Count == 0) return;
        column.Item().PaddingTop(2).Text(text =>
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0) text.Span("  •  ").FontColor("#90A4AE");
                text.Span(items[i]).FontSize(9.5f);
            }
        });
    }

    private static void AddTwoColumn(ColumnDescriptor column, List<(string Name, string Level)> items)
    {
        if (items.Count == 0) return;

        column.Item().PaddingTop(2).Column(col =>
        {
            foreach (var chunk in items.Chunk(2))
            {
                col.Item().Row(row =>
                {
                    foreach (var (name, level) in chunk)
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("• ").FontColor("#90A4AE");
                            text.Span(name).FontSize(9.5f);
                            if (!string.IsNullOrEmpty(level))
                                text.Span($"  ({level})").FontSize(9).FontColor("#78909C");
                        });
                    }
                    for (int i = chunk.Length; i < 2; i++)
                        row.RelativeItem();
                });
            }
        });
    }

    private static string Level(string level)
    {
        if (string.IsNullOrWhiteSpace(level)) return string.Empty;
        return proficiencyLabel(level);
    }

    private static string proficiencyLabel(string level)
    {
        return level;
    }

    private static string BuildList(params string?[] parts)
    {
        return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));
    }

    private static string BuildDateRange(string startMonth, string startYear, string endMonth, string endYear, bool isCurrent)
    {
        string start = $"{startMonth} {startYear}".Trim();
        string end = isCurrent ? "Present" : $"{endMonth} {endYear}".Trim();
        if (string.IsNullOrEmpty(start) && string.IsNullOrEmpty(end)) return string.Empty;
        return $"{start} – {end}".Trim();
    }

    private static CultureInfo GetResumeCulture(string resumeLanguage)
    {
        return resumeLanguage switch
        {
            "fr" => CultureInfo.GetCultureInfo("fr-FR"),
            "ar" => CultureInfo.GetCultureInfo("ar-SA"),
            _ => CultureInfo.GetCultureInfo("en-US")
        };
    }

    private static bool IsArabic(string resumeLanguage) => resumeLanguage == "ar";
}
