using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;

namespace CVGenerator.Services;

/// <summary>
/// Helpers to convert between FlowDocument XAML and plain text.
/// Used to store/restore rich text content and extract plain text for the PDF generator.
/// </summary>
public static class RichTextHelper
{
    public static string SerializeFlowDocument(FlowDocument document)
    {
        if (document == null) return string.Empty;
        return XamlWriter.Save(document);
    }

    public static FlowDocument? DeserializeFlowDocument(string xaml)
    {
        if (string.IsNullOrWhiteSpace(xaml)) return null;
        try
        {
            var doc = (FlowDocument)XamlReader.Parse(xaml);
            doc.PagePadding = new Thickness(0);
            doc.LineHeight = 1.2;
            return doc;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts plain text from serialized FlowDocument XAML (or returns the raw string
    /// if it isn't valid XAML).
    /// </summary>
    public static string GetPlainText(string xamlOrText)
    {
        if (string.IsNullOrWhiteSpace(xamlOrText)) return string.Empty;

        var doc = DeserializeFlowDocument(xamlOrText);
        if (doc == null)
            return xamlOrText;

        var textRange = new TextRange(doc.ContentStart, doc.ContentEnd);
        var text = textRange.Text ?? string.Empty;
        return text.Trim();
    }
}
