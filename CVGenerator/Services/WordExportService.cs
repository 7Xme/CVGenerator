using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using CVGenerator.Models;
using CVGenerator.Models.SectionModels;
using CVGenerator.Templates;
using Serilog;

namespace CVGenerator.Services;

/// <summary>
/// Minimal Word (.docx) exporter using the Open XML SDK.
/// Renders name, contact line, and each visible CV section.
/// </summary>
public class WordExportService
{
    public string GenerateDocx(CVData data, TemplateDefinition template, string outputPath)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using var doc = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var body = mainPart.Document.Body!;

            var pi = data.PersonalInfo;
            string name = string.IsNullOrWhiteSpace(pi.FullName)
                ? $"{pi.FirstName} {pi.LastName}".Trim()
                : pi.FullName;

            AddParagraph(body, name, bold: true, size: 28, color: template.PrimaryColor);

            var contact = new List<string>();
            if (!string.IsNullOrWhiteSpace(pi.PhonePrimary)) contact.Add(pi.PhonePrimary);
            if (!string.IsNullOrWhiteSpace(pi.Email)) contact.Add(pi.Email);
            if (!string.IsNullOrWhiteSpace(pi.City)) contact.Add(pi.City);
            if (contact.Count > 0)
                AddParagraph(body, string.Join("  |  ", contact), bold: false, size: 10);

            AddHorizontalRule(body);

            foreach (var section in BuildSections(data))
            {
                if (section.Hidden) continue;

                AddParagraph(body, section.Title, bold: true, size: 14, color: template.SecondaryColor);
                AddHorizontalRule(body);

                foreach (var entry in section.Entries)
                {
                    if (!string.IsNullOrEmpty(entry.TitleLine))
                        AddParagraph(body, entry.TitleLine, bold: true, size: 11);

                    if (!string.IsNullOrEmpty(entry.DetailLine))
                        AddParagraph(body, entry.DetailLine, bold: false, size: 10, color: "#607D8B");

                    foreach (var line in entry.DescriptionLines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            AddParagraph(body, "•  " + line, bold: false, size: 10);
                    }

                    AddParagraph(body, string.Empty, bold: false, size: 6);
                }
            }

            mainPart.Document.Save();
            Log.Information("Word document generated: {Path}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate Word document");
            throw new InvalidOperationException($"فشل توليد مستند Word: {ex.Message}", ex);
        }
    }

    private static List<SectionBlock> BuildSections(CVData data)
    {
        var sections = new List<SectionBlock>();
        var pi = data.PersonalInfo;

        if (!string.IsNullOrWhiteSpace(data.Summary))
        {
            sections.Add(new SectionBlock("Objective")
            {
                Entries = { new EntryBlock { DescriptionLines = { data.Summary } } }
            });
        }

        if (data.Experience.Count > 0)
        {
            sections.Add(new SectionBlock("Work Experience")
            {
                Entries = data.Experience.Select(e =>
                {
                    var lines = new List<string>();
                    lines.AddRange(e.Tasks);
                    if (!string.IsNullOrWhiteSpace(e.Description)) lines.Add(e.Description);
                    return new EntryBlock
                    {
                        TitleLine = $"{e.Position} — {e.Company}",
                        DetailLine = $"{e.StartDate} – {e.EndDate}",
                        DescriptionLines = lines
                    };
                }).ToList()
            });
        }

        if (data.Education.Count > 0)
        {
            sections.Add(new SectionBlock("Education")
            {
                Entries = data.Education.Select(e => new EntryBlock
                {
                    TitleLine = $"{e.Degree} — {e.Institution}",
                    DetailLine = e.EndYear,
                    DescriptionLines = new List<string>
                    {
                        string.IsNullOrWhiteSpace(e.FieldOfStudy) ? e.Description : e.FieldOfStudy
                    }
                }).ToList()
            });
        }

        if (data.Skills.Count > 0)
        {
            sections.Add(new SectionBlock("Skills")
            {
                Entries = data.Skills.Select(s => new EntryBlock
                {
                    TitleLine = string.IsNullOrWhiteSpace(s.Level) ? s.Name : $"{s.Name} — {s.Level}"
                }).ToList()
            });
        }

        if (data.Languages.Count > 0)
        {
            sections.Add(new SectionBlock("Languages")
            {
                Entries = data.Languages.Select(l => new EntryBlock
                {
                    TitleLine = string.IsNullOrWhiteSpace(l.Level) ? l.Name : $"{l.Name} — {l.Level}"
                }).ToList()
            });
        }

        if (data.Interests.Count > 0)
        {
            sections.Add(new SectionBlock("Interests")
            {
                Entries = { new EntryBlock { TitleLine = string.Join(", ", data.Interests) } }
            });
        }

        if (data.References.Count > 0)
        {
            sections.Add(new SectionBlock("References")
            {
                Entries = data.References.Select(r => new EntryBlock
                {
                    TitleLine = r.Name,
                    DetailLine = string.Join(" | ", new[] { r.Company, r.Phone, r.Email }.Where(x => !string.IsNullOrWhiteSpace(x)))
                }).ToList()
            });
        }

        if (data.Courses.Count > 0)
        {
            sections.Add(new SectionBlock("Courses")
            {
                Entries = data.Courses.Select(c => new EntryBlock
                {
                    TitleLine = c.Name,
                    DetailLine = $"{c.Institution} {c.Year}".Trim(),
                    DescriptionLines = new List<string> { c.Description }
                }).ToList()
            });
        }

        if (data.Achievements.Count > 0)
        {
            sections.Add(new SectionBlock("Achievements")
            {
                Entries = data.Achievements.Select(a => new EntryBlock
                {
                    TitleLine = a.Title,
                    DetailLine = a.Date,
                    DescriptionLines = new List<string> { a.Description }
                }).ToList()
            });
        }

        if (data.Publications.Count > 0)
        {
            sections.Add(new SectionBlock("Publications")
            {
                Entries = data.Publications.Select(p => new EntryBlock
                {
                    TitleLine = p.Title,
                    DetailLine = $"{p.Publisher} {p.Date}".Trim(),
                    DescriptionLines = new List<string> { p.Url }
                }).ToList()
            });
        }

        foreach (var cs in data.CustomSections)
        {
            sections.Add(new SectionBlock(cs.Title)
            {
                Entries = { new EntryBlock { DescriptionLines = { cs.Content } } }
            });
        }

        return sections;
    }

    private static void AddParagraph(Body body, string text, bool bold, int size, string? color = null)
    {
        if (string.IsNullOrEmpty(text)) return;

        var run = new Run(
            new RunProperties(
                new RunFonts { Ascii = "Segoe UI", HighAnsi = "Segoe UI", EastAsia = "Segoe UI" },
                new FontSize { Val = (size * 2).ToString() },
                new Color { Val = color ?? "000000" },
                bold ? new Bold() : null!),
            new Text(text) { Space = SpaceProcessingModeValues.Preserve });

        body.Append(new Paragraph(new ParagraphProperties(
            new SpacingBetweenLines { After = "120", Line = "276", LineRule = LineSpacingRuleValues.Auto }),
            run));
    }

    private static void AddHorizontalRule(Body body)
    {
        var paragraph = new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "40", After = "160" },
                new ParagraphBorders(new BottomBorder
                {
                    Val = BorderValues.Single,
                    Size = 6,
                    Color = "5C6BC0"
                })));
        body.Append(paragraph);
    }

    private class SectionBlock
    {
        public string Title { get; }
        public List<EntryBlock> Entries { get; set; } = new();
        public bool Hidden { get; }

        public SectionBlock(string title, bool hidden = false)
        {
            Title = title;
            Hidden = hidden;
        }
    }

    private class EntryBlock
    {
        public string TitleLine { get; set; } = string.Empty;
        public string DetailLine { get; set; } = string.Empty;
        public List<string> DescriptionLines { get; set; } = new();
    }
}
