using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CVGenerator.Templates;

namespace CVGenerator.ViewModels;

public enum ExportFormat
{
    Pdf,
    PowerPoint,
    Word
}

public partial class TemplateCardViewModel : ObservableObject
{
    public TemplateDefinition Template { get; }

    [ObservableProperty]
    private bool _isSelected;

    public TemplateCardViewModel(TemplateDefinition template)
    {
        Template = template;
    }
}

public partial class Step3TemplateViewModel : ObservableObject
{
    public ObservableCollection<TemplateCardViewModel> Templates { get; } = new();

    private readonly ManualWizardViewModel _wizard;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public Step3TemplateViewModel(ManualWizardViewModel wizard)
    {
        _wizard = wizard;
        foreach (var t in TemplateCatalog.All)
            Templates.Add(new TemplateCardViewModel(t));

        Templates[0].IsSelected = true;
    }

    public TemplateDefinition SelectedTemplate => Templates.FirstOrDefault(t => t.IsSelected)?.Template ?? TemplateCatalog.Default;

    public void SelectTemplate(TemplateDefinition template)
    {
        foreach (var card in Templates)
            card.IsSelected = card.Template == template;
    }

    [RelayCommand]
    private void SelectTemplate(TemplateCardViewModel card) => SelectTemplate(card.Template);

    [RelayCommand]
    private async Task ExportPdf() => await ExportAsync(ExportFormat.Pdf);

    [RelayCommand]
    private async Task ExportPowerPoint() => await ExportAsync(ExportFormat.PowerPoint);

    [RelayCommand]
    private async Task ExportWord() => await ExportAsync(ExportFormat.Word);

    private async Task ExportAsync(ExportFormat format)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await Task.Run(() => _wizard.ExportData(SelectedTemplate, format));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
