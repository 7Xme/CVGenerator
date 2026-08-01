using System.Diagnostics;
using System.IO;
using System.Windows;
using Serilog;

namespace CVGenerator.Views;

public partial class PrintPreviewWindow : Window
{
    private readonly string _pptxPath;

    public PrintPreviewWindow(string pptxPath)
    {
        InitializeComponent();
        _pptxPath = pptxPath;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await webView.EnsureCoreWebView2Async();
            webView.CoreWebView2.NavigateToString(
                "<html dir='rtl'><body style='font-family: Segoe UI; text-align: center; padding-top: 100px; color: #7F8C8D;'>" +
                "<h2>📊 معاينة PowerPoint</h2>" +
                "<p>لفتح الملف في PowerPoint، اضغط على الزر أدناه.</p>" +
                "<p style='font-size: 12px; color: #BDC3C7;'>" + Path.GetFileName(_pptxPath) + "</p>" +
                "</body></html>");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "WebView2 initialization failed");
        }
    }

    private void btnPrint_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _pptxPath,
                Verb = "Print",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            Log.Information("Printing: {Path}", _pptxPath);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشلت الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnOpenInPPT_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _pptxPath,
                UseShellExecute = true
            });
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل الفتح: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnClose_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
