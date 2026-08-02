using System.Globalization;
using System.IO;
using CVGenerator.Localization;
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

    static PdfGeneratorService()
    {
        // QuestPDF 2024.3.0 ships under the MIT license; the community license
        // suppresses the license validation dialog that would otherwise block PDF generation.
        QuestPDF.Settings.License = LicenseType.Community;

        // Enable lookup of Windows system fonts so Arabic text can be rendered
        // with a font that actually contains Arabic glyphs (e.g. Segoe UI).
        QuestPDF.Settings.UseEnvironmentFonts = true;
    }

    public PdfGeneratorService()
    {
    }

    public string GeneratePdf(CVData data, TemplateDefinition template,
        IReadOnlyList<SectionBase> sections, string outputPath)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            var resumeLang = string.IsNullOrWhiteSpace(data.PersonalInfo.ResumeLanguage)
                ? "en"
                : data.PersonalInfo.ResumeLanguage;
            var isRtl = resumeLang == "ar";
            // "en" / "fr" / "ar" are the culture suffixes used by the .resx resource files.
            var cultureName = resumeLang == "fr" ? "fr" : resumeLang == "ar" ? "ar" : "en";
            var fontFamily = isRtl ? "Segoe UI" : string.IsNullOrWhiteSpace(template.FontFamily) ? "Segoe UI" : template.FontFamily;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(text => text
                        .FontFamily(fontFamily)
                        .FontSize(10)
                        .FontColor("#37474F"));

                    // Mirror the whole document layout for Arabic resumes.
                    if (isRtl)
                        page.ContentFromRightToLeft();

                    page.Header().Column(header =>
                    {
                        header.Spacing(6);
                        RenderHeader(header, data, template, isRtl, cultureName);
                    });

                    page.Content().PaddingTop(12).Column(column =>
                    {
                        column.Spacing(10);
                        foreach (var section in sections)
                        {
                            if (!section.IsVisibleInCv) continue;
                            RenderSection(column, section, template, isRtl, cultureName);
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.CurrentPageNumber().FontColor("#90A4AE").FontSize(8);
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

    /// <summary>
    /// Returns a Windows system font with Arabic glyph support for RTL documents,
    /// otherwise preserves the template's font family.
    /// </summary>
    private static string GetCultureFont(bool isRtl, string templateFontFamily)
    {
        return isRtl ? "Segoe UI" : templateFontFamily;
    }

    private static void RenderHeader(ColumnDescriptor header, CVData data, TemplateDefinition template, bool isRtl, string cultureName)
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
                if (isRtl) text.DirectionFromRightToLeft();
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
                if (isRtl) text.DirectionFromRightToLeft();
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
                if (isRtl) text.DirectionFromRightToLeft();
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

    private static void RenderSection(ColumnDescriptor column, SectionBase section, TemplateDefinition template, bool isRtl, string cultureName)
    {
        if (string.IsNullOrWhiteSpace(section.Title)) return;

        column.Item().PaddingTop(4).Column(item =>
        {
            item.Spacing(4);

            // Localize the section title when it still holds a default (i.e. un-renamed) label.
            string title = LocalizeSectionTitle(section, cultureName);

            item.Item().Text(text =>
            {
                text.Span(section.Icon + "  ").FontSize(11);
                text.Span(title).FontSize(13).Bold().FontColor(template.SecondaryColor);
                if (isRtl) text.DirectionFromRightToLeft();
            });
            item.Item().LineHorizontal(0.8f).LineColor(template.PrimaryColor);

            switch (section)
            {
                case ObjectiveSection obj:
                    AddParagraph(item, obj.Content, isRtl);
                    break;

                case InterestsSection ints:
                    AddInlineList(item, ints.Tags.ToList(), isRtl);
                    break;

                case CustomSection custom:
                    AddParagraph(item, custom.Content, isRtl);
                    break;

                case WorkExperienceSection we:
                    foreach (var e in we.Entries)
                    {
                        RenderEntry(item, BuildList(e.JobTitle, e.Employer, e.City),
                            BuildDateRange(e.StartMonth, e.StartYear, e.EndMonth, e.EndYear, e.IsCurrent, cultureName),
                            e.Description, isRtl);
                    }
                    break;

                case EducationSection edu:
                    foreach (var e in edu.Entries)
                    {
                        RenderEntry(item, BuildList(e.Degree, e.Institution, e.FieldOfStudy, e.City),
                            BuildDateRange(e.StartMonth, e.StartYear, e.EndMonth, e.EndYear, false, cultureName),
                            e.Description, isRtl);
                    }
                    break;

                case SkillsSection skills:
                    AddTwoColumn(item, skills.Entries
                        .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                        .Select(s => (s.Name, Level(s.Level))).ToList(), isRtl);
                    break;

                case LanguagesSection langs:
                    AddTwoColumn(item, langs.Entries
                        .Where(l => !string.IsNullOrWhiteSpace(l.Name))
                        .Select(l => (l.Name, Level(l.Level))).ToList(), isRtl);
                    break;

                case ReferencesSection refs:
                    foreach (var r in refs.Entries.Where(r => !string.IsNullOrWhiteSpace(r.Name)))
                    {
                        AddParagraph(item, BuildList(r.Name, r.Company, r.Phone, r.Email), isRtl);
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

    /// <summary>
    /// Maps a section's Kind to its localized title. If the current title has not
    /// been customized by the user (i.e. it matches the default label in any of the
    /// three supported languages), it is replaced with the resume-culture translation.
    /// User-renamed titles are preserved as-is.
    /// </summary>
    private static string LocalizeSectionTitle(SectionBase section, string cultureName)
    {
        var key = section.Kind switch
        {
            SectionKind.ResumeObjective => "Section.Objective",
            SectionKind.WorkExperience => "Section.WorkExperience",
            SectionKind.Education => "Section.Education",
            SectionKind.Interests => "Section.Interests",
            SectionKind.References => "Section.References",
            SectionKind.Skills => "Section.Skills",
            SectionKind.Languages => "Section.Languages",
            SectionKind.Courses => "Section.Courses",
            SectionKind.Achievements => "Section.Achievements",
            SectionKind.Publications => "Section.Publications",
            _ => null
        };

        if (key == null) return section.Title;

        // Default labels in each supported language for this section kind.
        var locals = LocalizationService.Instance;
        string defaultEn = locals.GetString(key, "en");
        string defaultFr = locals.GetString(key, "fr");
        string defaultAr = locals.GetString(key, "ar");

        // Only localize un-renamed default titles; keep custom titles untouched.
        if (section.Title == defaultEn || section.Title == defaultFr || section.Title == defaultAr)
            return locals.GetString(key, cultureName);

        return section.Title;
    }

    private static void RenderEntry(ColumnDescriptor column, string title, string dateRange, string description, bool isRtl)
    {
        column.Item().Column(item =>
        {
            item.Spacing(1);
            item.Item().Row(row =>
            {
                if (isRtl)
                {
                    // Mirror: title on the right, date on the far left.
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span(title).FontSize(10.5f).Bold().FontColor("#263238");
                        text.DirectionFromRightToLeft();
                    });
                    if (!string.IsNullOrWhiteSpace(dateRange))
                    {
                        row.ConstantItem(140).AlignLeft().Text(text =>
                            text.Span(dateRange).FontSize(9).FontColor(templateHint("date")));
                    }
                }
                else
                {
                    row.RelativeItem().Text(text => text.Span(title).FontSize(10.5f).Bold().FontColor("#263238"));
                    if (!string.IsNullOrWhiteSpace(dateRange))
                    {
                        row.ConstantItem(140).AlignRight().Text(text =>
                            text.Span(dateRange).FontSize(9).FontColor(templateHint("date")));
                    }
                }
            });
            AddParagraph(item, description, isRtl);
        });
    }

    private static string templateHint(string _) => "#78909C";

    private static void AddParagraph(ColumnDescriptor column, string content, bool isRtl)
    {
        var text = RichTextHelper.GetPlainText(content);
        if (string.IsNullOrWhiteSpace(text)) return;

        column.Item().PaddingTop(2).Text(x =>
        {
            x.Span(text).FontSize(9.5f).LineHeight(1.25f);
            if (isRtl) x.DirectionFromRightToLeft();
        });
    }

    private static void AddInlineList(ColumnDescriptor column, List<string> items, bool isRtl)
    {
        if (items.Count == 0) return;
        column.Item().PaddingTop(2).Text(text =>
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (i > 0) text.Span("  •  ").FontColor("#90A4AE");
                text.Span(items[i]).FontSize(9.5f);
            }
            if (isRtl) text.DirectionFromRightToLeft();
        });
    }

    private static void AddTwoColumn(ColumnDescriptor column, List<(string Name, string Level)> items, bool isRtl)
    {
        if (items.Count == 0) return;

        column.Item().PaddingTop(2).Column(col =>
        {
            foreach (var chunk in items.Chunk(2))
            {
                col.Item().Row(row =>
                {
                    for (int i = 0; i < chunk.Length; i++)
                    {
                        var (name, level) = chunk[isRtl ? chunk.Length - 1 - i : i];
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("• ").FontColor("#90A4AE");
                            text.Span(name).FontSize(9.5f);
                            if (!string.IsNullOrEmpty(level))
                                text.Span($"  ({level})").FontSize(9).FontColor("#78909C");
                            if (isRtl) text.DirectionFromRightToLeft();
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

    private static string BuildDateRange(string startMonth, string startYear, string endMonth, string endYear, bool isCurrent, string cultureName)
    {
        string start = $"{startMonth} {startYear}".Trim();
        string end = isCurrent
            ? LocalizationService.Instance.GetString("Common.Present", cultureName)
            : $"{endMonth} {endYear}".Trim();
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