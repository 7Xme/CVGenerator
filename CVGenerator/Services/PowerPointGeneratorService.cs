using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using CVGenerator.Models;
using Serilog;

namespace CVGenerator.Services;

public class PowerPointGeneratorService
{
    private readonly string? _templatePath;
    private readonly string _outputDirectory;
    private const string PlaceholderPrefix = "{{";
    private const string PlaceholderSuffix = "}}";

    public PowerPointGeneratorService(string? templatePath = null, string? outputDirectory = null)
    {
        _templatePath = templatePath;
        _outputDirectory = outputDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
        Directory.CreateDirectory(_outputDirectory);
    }

    public string GenerateCV(CVData data, string? outputFileName = null)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        outputFileName ??= $"CV_{SanitizeFileName(data.PersonalInfo.FullName)}_{DateTime.Now:yyyyMMdd_HHmmss}.pptx";
        var outputPath = Path.Combine(_outputDirectory, outputFileName);

        try
        {
            if (_templatePath != null && File.Exists(_templatePath))
            {
                Log.Information("Using template: {Template}", _templatePath);
                File.Copy(_templatePath, outputPath, true);
                FillTemplate(data, outputPath);
            }
            else
            {
                Log.Information("Creating presentation from scratch");
                CreateFromScratch(data, outputPath);
            }

            Log.Information("PPTX generated: {Path}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to generate PowerPoint");
            throw new InvalidOperationException($"فشل توليد PowerPoint: {ex.Message}", ex);
        }
    }

    private static void FillTemplate(CVData data, string pptxPath)
    {
        using var doc = PresentationDocument.Open(pptxPath, true);
        var presPart = doc.PresentationPart!;

        foreach (var slidePart in presPart.SlideParts)
        {
            var slide = slidePart.Slide;
            ReplacePlaceholders(slide, data);

            foreach (var shape in slide.Descendants<Shape>())
            {
                if (shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?
                    .PlaceholderShape != null)
                {
                    var texts = shape.Descendants<A.Text>();
                    foreach (var text in texts)
                    {
                        if (text.Text.Contains(PlaceholderPrefix))
                        {
                            text.Text = ResolvePlaceholder(text.Text, data);
                        }
                    }
                }
            }

            slide.Save();
        }
    }

    private static void ReplacePlaceholders(Slide slide, CVData data)
    {
        var texts = slide.Descendants<A.Text>();
        foreach (var text in texts)
        {
            if (!text.Text.Contains(PlaceholderPrefix)) continue;
            text.Text = ResolvePlaceholder(text.Text, data);
        }
    }

    private static string ResolvePlaceholder(string input, CVData data)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var pi = data.PersonalInfo;
        return input
            .Replace("{{FULL_NAME}}", pi.FullName)
            .Replace("{{FULL_NAME_LATIN}}", pi.FullNameLatin)
            .Replace("{{PHONE}}", pi.PhonePrimary)
            .Replace("{{PHONE_SECONDARY}}", pi.PhoneSecondary)
            .Replace("{{EMAIL}}", pi.Email)
            .Replace("{{NATIONAL_ID}}", pi.NationalId)
            .Replace("{{ADDRESS}}", pi.Address)
            .Replace("{{DRIVING_LICENSE}}", pi.DrivingLicense)
            .Replace("{{DATE_OF_BIRTH}}", pi.DateOfBirth)
            .Replace("{{SUMMARY}}", data.Summary);
    }

    private static void CreateFromScratch(CVData data, string outputPath)
    {
        using var presentation = PresentationDocument.Create(outputPath, PresentationDocumentType.Presentation);
        var presPart = presentation.AddPresentationPart();
        presPart.Presentation = new Presentation();

        var slideIdList = new SlideIdList();

        SlidePart slidePart = CreateSlide(presPart, data);

        var slideId = new SlideId
        {
            Id = 256,
            RelationshipId = presPart.GetIdOfPart(slidePart)
        };
        slideIdList.Append(slideId);

        presPart.Presentation.SlideIdList = slideIdList;
        presPart.Presentation.Save();
    }

    private static SlidePart CreateSlide(PresentationPart presPart, CVData data)
    {
        SlidePart slidePart = presPart.AddNewPart<SlidePart>();

        var shapeTree = new ShapeTree(
            new NonVisualGroupShapeProperties(
                new NonVisualDrawingProperties { Id = 1, Name = "CV Slide" },
                new NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties(new A.TransformGroup())
        );

        slidePart.Slide = new Slide(new CommonSlideData(shapeTree));

        var slide = slidePart.Slide;

        double yPos = 10.0;

        yPos = AddTitleSection(shapeTree, data, yPos);
        yPos = AddSummarySection(shapeTree, data, yPos);
        yPos = AddEducationSection(shapeTree, data, yPos);
        yPos = AddExperienceSection(shapeTree, data, yPos);
        yPos = AddSkillsSection(shapeTree, data, yPos);
        AddLanguagesSection(shapeTree, data, yPos);

        slide.Save();
        return slidePart;
    }

    private static double AddTitleSection(ShapeTree shapeTree, CVData data, double yPos)
    {
        var pi = data.PersonalInfo;

        AddTextBox(shapeTree, pi.FullName, yPos, 0.5, 9.0, 1.0, "2C3E50", 28, true, A.TextAlignmentTypeValues.Center);
        yPos += 1.2;

        AddTextBox(shapeTree, $"📧 {pi.Email}  |  📞 {pi.PhonePrimary}  |  📍 {pi.Address}",
            yPos, 0.5, 9.0, 0.5, "7F8C8D", 12, false, A.TextAlignmentTypeValues.Center);
        yPos += 0.7;

        AddLineSeparator(shapeTree, yPos);
        yPos += 0.3;

        return yPos;
    }

    private static double AddSummarySection(ShapeTree shapeTree, CVData data, double yPos)
    {
        if (string.IsNullOrWhiteSpace(data.Summary)) return yPos;

        AddTextBox(shapeTree, "🎯 ملخص", yPos, 0.5, 9.0, 0.4, "2C3E50", 16, true, A.TextAlignmentTypeValues.Left);
        yPos += 0.5;

        AddTextBox(shapeTree, data.Summary, yPos, 0.5, 9.0, 0.6, "34495E", 11, false, A.TextAlignmentTypeValues.Left);
        yPos += 0.7;

        return yPos;
    }

    private static double AddEducationSection(ShapeTree shapeTree, CVData data, double yPos)
    {
        if (data.Education.Count == 0) return yPos;

        AddTextBox(shapeTree, "🎓 التعليم", yPos, 0.5, 9.0, 0.4, "2C3E50", 16, true, A.TextAlignmentTypeValues.Left);
        yPos += 0.5;

        foreach (var edu in data.Education)
        {
            string text = $"{edu.Degree} - {edu.Institution} ({edu.Year})";
            if (!string.IsNullOrEmpty(edu.Mention))
                text += $" | {edu.Mention}";

            AddTextBox(shapeTree, $"• {text}", yPos, 0.8, 8.5, 0.4, "34495E", 11, false, A.TextAlignmentTypeValues.Left);
            yPos += 0.45;
        }

        yPos += 0.2;
        return yPos;
    }

    private static double AddExperienceSection(ShapeTree shapeTree, CVData data, double yPos)
    {
        if (data.Experience.Count == 0) return yPos;

        AddTextBox(shapeTree, "💼 الخبرات العملية", yPos, 0.5, 9.0, 0.4, "2C3E50", 16, true, A.TextAlignmentTypeValues.Left);
        yPos += 0.5;

        foreach (var exp in data.Experience)
        {
            string header = $"{exp.Position} @ {exp.Company} ({exp.StartDate} - {exp.EndDate})";
            AddTextBox(shapeTree, $"• {header}", yPos, 0.8, 8.5, 0.4, "2C3E50", 12, true, A.TextAlignmentTypeValues.Left);
            yPos += 0.4;

            foreach (var task in exp.Tasks)
            {
                AddTextBox(shapeTree, $"  - {task}", yPos, 1.0, 8.0, 0.35, "7F8C8D", 10, false, A.TextAlignmentTypeValues.Left);
                yPos += 0.35;
            }
        }

        yPos += 0.2;
        return yPos;
    }

    private static double AddSkillsSection(ShapeTree shapeTree, CVData data, double yPos)
    {
        if (data.Skills.Count == 0) return yPos;

        AddTextBox(shapeTree, "🛠️ المهارات", yPos, 0.5, 9.0, 0.4, "2C3E50", 16, true, A.TextAlignmentTypeValues.Left);
        yPos += 0.5;

        var skillsText = string.Join("  |  ", data.Skills.Select(s =>
            string.IsNullOrEmpty(s.Level) ? s.Name : $"{s.Name}: {s.Level}"));

        AddTextBox(shapeTree, skillsText, yPos, 0.5, 9.0, 0.4, "34495E", 11, false, A.TextAlignmentTypeValues.Left);
        yPos += 0.5;

        return yPos;
    }

    private static void AddLanguagesSection(ShapeTree shapeTree, CVData data, double yPos)
    {
        if (data.Languages.Count == 0) return;

        AddTextBox(shapeTree, "🌍 اللغات", yPos, 0.5, 9.0, 0.4, "2C3E50", 16, true, A.TextAlignmentTypeValues.Left);
        yPos += 0.5;

        var langsText = string.Join("  |  ", data.Languages.Select(l =>
            string.IsNullOrEmpty(l.Level) ? l.Name : $"{l.Name}: {l.Level}"));

        AddTextBox(shapeTree, langsText, yPos, 0.5, 9.0, 0.4, "34495E", 11, false, A.TextAlignmentTypeValues.Left);
    }

    private static void AddTextBox(ShapeTree shapeTree, string text, double left, double top,
        double width, double height, string colorHex, int fontSize, bool bold,
        A.TextAlignmentTypeValues alignment)
    {
        var transform = new A.Transform2D(
            new A.Offset { X = Inches(left), Y = Inches(top) },
            new A.Extents { Cx = Inches(width), Cy = Inches(height) });

        var runProperties = new A.RunProperties
        {
            Language = "ar-SA",
            FontSize = fontSize * 100,
            Bold = bold
        };
        runProperties.Append(new A.SolidFill
        {
            RgbColorModelHex = new A.RgbColorModelHex { Val = colorHex }
        });

        var paragraphProperties = new A.ParagraphProperties();
        if (alignment != A.TextAlignmentTypeValues.Left)
            paragraphProperties.Alignment = alignment;

        var paragraph = new A.Paragraph();
        paragraph.ParagraphProperties = paragraphProperties;
        paragraph.Append(new A.Run(runProperties, new A.Text { Text = text }));

        var body = new A.TextBody(paragraph);
        body.BodyProperties = new A.BodyProperties();

        var shape = new Shape(
            new ShapeProperties(transform, new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle }),
            body
        );

        shapeTree.Append(shape);
    }

    private static void AddLineSeparator(ShapeTree shapeTree, double yPos)
    {
        var transform = new A.Transform2D(
            new A.Offset { X = Inches(0.5) },
            new A.Extents { Cx = Inches(9.0), Cy = Inches(0.02) }
        );

        var shape = new Shape(
            new ShapeProperties(transform,
                new A.SolidFill { RgbColorModelHex = new A.RgbColorModelHex { Val = "BDC3C7" } },
                new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle })
        );

        shapeTree.Append(shape);
    }

    private static Int64Value Inches(double inches) => new((long)(inches * 914400));

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Untitled";
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "Untitled" : sanitized.Trim();
    }
}
