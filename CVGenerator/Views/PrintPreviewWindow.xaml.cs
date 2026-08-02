using System.Diagnostics;
using System.IO;
using System.Windows;
using CVGenerator.Localization;
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
            var loc = LocalizationService.Instance;
            var title = loc.GetString("PrintPreview.Title");
            var body = loc.GetString("PrintPreview.Body");
            var dir = loc.WindowFlowDirection == FlowDirection.RightToLeft ? "rtl" : "ltr";
            webView.CoreWebView2.NavigateToString(
                $"<html dir='{dir}'><body style='font-family: Segoe UI; text-align: center; padding-top: 100px; color: #7F8C8D;'>" +
                $"<h2>📊 {title}</h2>" +
                $"<p>{body}</p>" +
                $"<p style='font-size: 12px; color: #BDC3C7;'>{Path.GetFileName(_pptxPath)}</p>" +
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
            var loc = LocalizationService.Instance;
            var msg = string.Format(loc.GetString("PrintPreview.PrintError"), ex.Message);
            MessageBox.Show(msg, loc.GetString("PrintPreview.PrintErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
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
            var loc = LocalizationService.Instance;
            var msg = string.Format(loc.GetString("PrintPreview.OpenError"), ex.Message);
            MessageBox.Show(msg, loc.GetString("PrintPreview.PrintErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnClose_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
