using System.Windows;
using System.Windows.Controls;

namespace CVGenerator.Converters;

/// <summary>
/// Swaps the manual-wizard body between the three step views based on the
/// current step number (int) bound as the ContentControl.Content.
/// </summary>
public class StepContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Step1Template { get; set; }
    public DataTemplate? Step2Template { get; set; }
    public DataTemplate? Step3Template { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container)
    {
        return item switch
        {
            1 => Step1Template,
            2 => Step2Template,
            3 => Step3Template,
            _ => Step1Template
        };
    }
}
