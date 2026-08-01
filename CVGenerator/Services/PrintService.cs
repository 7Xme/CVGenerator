using System.Diagnostics;
using System.IO;
using Serilog;

namespace CVGenerator.Services;

public class PrintService
{
    public void PrintPowerPoint(string pptxPath)
    {
        if (!File.Exists(pptxPath))
            throw new FileNotFoundException("ملف PowerPoint غير موجود", pptxPath);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pptxPath,
                Verb = "Print",
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            };

            using var process = Process.Start(psi);
            Log.Information("Printing started for: {Path}", pptxPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print PowerPoint: {Path}", pptxPath);
            throw new InvalidOperationException(
                "فشلت عملية الطباعة. تأكد من تثبيت Microsoft PowerPoint.", ex);
        }
    }

    public void OpenPowerPoint(string pptxPath)
    {
        if (!File.Exists(pptxPath))
            throw new FileNotFoundException("ملف PowerPoint غير موجود", pptxPath);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pptxPath,
                UseShellExecute = true
            };

            Process.Start(psi);
            Log.Information("Opened PowerPoint: {Path}", pptxPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open PowerPoint: {Path}", pptxPath);
            throw new InvalidOperationException(
                "فشل فتح ملف PowerPoint. تأكد من تثبيت Microsoft PowerPoint.", ex);
        }
    }

    public string ExportToPdf(string pptxPath)
    {
        Log.Warning("PDF export requires Aspose.Slides or Microsoft Office Interop.");
        throw new NotImplementedException(
            "تصدير PDF يتطلب تثبيت Aspose.Slides أو Microsoft Office Interop.");
    }
}
